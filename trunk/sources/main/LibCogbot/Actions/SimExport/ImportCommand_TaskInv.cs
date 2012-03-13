using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.IO;
using cogbot.Actions.SimExport;
using cogbot.Listeners;
using cogbot.TheOpenSims;
using MushDLR223.Utilities;
using OpenMetaverse;
using OpenMetaverse.Assets;
using OpenMetaverse.StructuredData;

using MushDLR223.ScriptEngines;

namespace cogbot.Actions.SimExport
{

    public partial class ImportCommand
    {
        public class TaskObject
        {
            public bool IsPlaced = false;
            public PrimToCreate Rezzed
            {
                get
                {
                    return Running.GetOldPrim(RezzedID);
                }
            }
            public PrimToCreate InsideOf
            {
                get
                {
                    return Running.GetOldPrim(OldParent);
                }
            }
            public override bool Equals(object obj)
            {
                TaskObject tob = obj as TaskObject;
                return tob != null && tob.TaskItemID == TaskItemID;
            }
            public override int GetHashCode()
            {
                return TaskItemID.GetHashCode();
            }
            public override string ToString()
            {
                return "TaskObj " + Rezzed + " " + TaskItemID + "/" + AssetUUID + " inside " + InsideOf;
            }
            public uint OldLid = 0;
            /// <summary>
            /// Prim it was inside
            /// </summary>
            public UUID OldParent = UUID.Zero;
            /// <summary>
            /// When we rezed it - therefore the LLSD File
            /// </summary>
            public UUID RezzedID = UUID.Zero;
            /// <summary>
            /// AssetUUID (found in TaskInv)
            /// </summary>
            public UUID AssetUUID = UUID.Zero;
            /// <summary>
            /// ItemID (folder Item in TaskInv)
            /// </summary>
            public UUID TaskItemID = UUID.Zero;
        }
        public void ImportTaskObjects(ImportSettings importSettings)
        {
            DateTime lastProgressNotice = DateTime.Now;
            int incomplete = 0;
            var agentSyncFolderHolder = ExportCommand.Running.FolderCalled("TaskInvHolder");
            int created = 0;
            var tos = LocalScene.TaskObjects;
            foreach (var file in Directory.GetFiles(ExportCommand.dumpDir, "*.taskobj"))
            {
                string[] c = File.ReadAllText(file).Split(',');
                TaskObject to = new TaskObject()
                                    {
                                        OldLid = uint.Parse(c[0]),
                                        RezzedID = UUID.Parse(c[2]),
                                        OldParent = UUID.Parse(c[3]),
                                        AssetUUID = UUID.Parse(c[4]),
                                        TaskItemID = UUID.Parse(c[5])
                                    };
                tos.Add(to);
                created++;
            }
            foreach (TaskObject o in tos)
            {
                o.Rezzed.SetIsAsset();
            }
            foreach (TaskObject o in tos)
            {
                foreach(var b in o.InsideOf.TaskInventory)
                {
                    InventoryItem i = b as InventoryItem;
                    if (i == null) continue;
                    if (CogbotHelpers.IsNullOrZero(i.AssetUUID))
                    {
                        if (i.UUID == o.TaskItemID)
                        {
                            i.AssetUUID = o.RezzedID;
                            o.IsPlaced = true;
                            break;
                        }
                    }
                }
            }
            foreach (TaskObject o in tos)
            {
                if (!o.IsPlaced)
                {
                    Failure("UNPLACED: " + o);
                }
            }
        }
        public void DivideTaskObjects(ImportSettings importSettings)
        {
        }

        private void ImportTaskFiles(ImportSettings importSettings, bool createObjects)
        {
            int incomplete = 1;
            int maxRetries = 10;
            while (incomplete > 0 && maxRetries-- > 0)
            {
                incomplete = ImportTaskFiles0(importSettings, createObjects);
                WriteLine("ImportTaskFiles Incomplete=" + incomplete);
            }
        }
        private int ImportTaskFiles0(ImportSettings importSettings, bool createObjects)
        {
            //if (IsLocalScene) return 0;
            DateTime lastProgressNotice = DateTime.Now;
            int incomplete = 0;
            var agentSyncFolderHolder = ExportCommand.Running.FolderCalled("TaskInvHolder");
            int created = 0;
            foreach (var file in Directory.GetFiles(ExportCommand.dumpDir, "*.task"))
            {
                string fileUUID = Path.GetFileNameWithoutExtension(Path.GetFileName(file));
                var ptc = APrimToCreate(UUID.Parse(fileUUID));
                if (++created % 25 == 0) WriteLine("tasked " + created);
                if (ptc.PackedInsideNow) continue;
                if (ptc.TaskInvComplete) continue;
                string taskDataS = File.ReadAllText(file);
                if (string.IsNullOrEmpty(taskDataS))
                {
                    ptc.TaskInvComplete = true;
                    continue;
                }
                ptc.CreateWorkflow(agentSyncFolderHolder);
                if (!ptc.LoadOSD(taskDataS, WriteLine))
                {
                    //Failure("FAILED: LoadOSD " + ptc);
                    incomplete++;
                    continue;
                }
                if (!ptc.SyncToAgentFolder(WriteLine, createObjects))
                {
                    //Failure("FAILED: SyncToAgentFolder " + ptc);
                    if (ptc.succeeded == 0) continue;
                }
                if (!ptc.SyncToObject(WriteLine, createObjects))
                {
                    //Failure("FAILED: SyncToObject " + ptc);
                    if (ptc.succeeded == 0) continue;
                }
                if (!ptc.Complete)
                {
                    Failure("INCOMPLETE: " + ptc);
                    incomplete++;
                }
                else
                {
                    Success("COMPLETE: " + ptc);
                }
                if (!IsLocalScene) Success("............");
                if (lastProgressNotice.AddSeconds(30) < DateTime.Now)
                {
                    lastProgressNotice = DateTime.Now;
                    WriteLine("Task created " + created + " incomplete=" + incomplete);
                }
            }
            return incomplete;
        }

        public partial class PrimToCreate
        {
            internal List<InventoryBase> objectinventory;
            private ManualResetEvent taskInvRequestRelpy = null;
            private ulong _xferID;
            public List<InventoryBase> TaskInventory
            {
                get
                {
                    if (objectinventory == null)
                    {
                        if (IsLocalScene)
                        {
                            if (TaskItemsToCreate == null) return null;
                            objectinventory = new List<InventoryBase>();
                            lock (TaskItemsToCreate) foreach (var toCreate in TaskItemsToCreate)
                            {
                                objectinventory.Add(toCreate.ToInventoryBase());
                            }
                            return objectinventory;
                        }
                        if (!RequestNewTaskInventory().WaitOne(TimeSpan.FromSeconds(10)))
                        {
                            Running.WriteLine("Unable to retrieve TaskInv for " + ToString());
                        }
                    }
                    return objectinventory;
                }
            }

            public ManualResetEvent RequestNewTaskInventory()
            {
                objectinventory = null;
                taskInvRequestRelpy = taskInvRequestRelpy ?? new ManualResetEvent(false);
                taskInvRequestRelpy.Reset();
                var man = this.Inventory;
                man.TaskInventoryReply += ti_callback;
                man.RequestTaskInventory(NewLocalID);
                return taskInvRequestRelpy;

            }

            internal InventoryManager Inventory
            {
                get { return Client.Inventory; }
            }

            private void ti_callback(object sender, TaskInventoryReplyEventArgs e)
            {
                if (e.ItemID == NewID)
                {
                    InventoryManager man = Inventory;
                    String filename = e.AssetFilename;
                    man.TaskInventoryReply -= ti_callback;

                    if (!String.IsNullOrEmpty(filename))
                    {
                        Running.Client.Assets.XferReceived += xferCallback;

                        // Start the actual asset xfer
                        _xferID = Running.Client.Assets.RequestAssetXfer(filename, true, false, UUID.Zero, AssetType.Unknown,
                                                                 true);
                    }
                    else
                    {
                        Logger.DebugLog("Task is empty for " + NewID, Running.Client);
                        if (TaskInventoryLikely)
                        {
                            objectinventory = SimObjectImpl.ERROR_TASK_INV;
                        }
                        else
                        {
                            objectinventory = SimObjectImpl.EMPTY_TASK_INV;
                        }
                    }
                }
            }

            protected bool TaskInventoryLikely
            {
                get { return true; }
            }

            private void xferCallback(object sender, XferReceivedEventArgs e)
            {
                if (e.Xfer.XferID == _xferID)
                {
                    Running.Client.Assets.XferReceived -= xferCallback;
                    if (e.Xfer.Error != TransferError.None)
                    {
                        objectinventory = SimObjectImpl.ERROR_TASK_INV;
                        taskInvRequestRelpy.Set();
                        return;
                    }
                    String taskList = Utils.BytesToString(e.Xfer.AssetData);
                    objectinventory = InventoryManager.ParseTaskInventory(taskList);
                    taskInvRequestRelpy.Set();
                }
            }

            public int succeeded = 0;
            public int failed = 0;

            public BotClient Client
            {
                get { return Running.Client; }
            }

            public UUID AgentSyncFolder { get; set; }

            internal List<InventoryBase> _contents;
            public List<InventoryBase> AgentContents
            {
                get
                {
                    if (_contents == null)
                    {
                        _contents = Inventory.FolderContents(AgentSyncFolder, Client.Self.AgentID, false, true,
                                                                    InventorySortOrder.ByDate, 10000);

                        if (_contents == null) _contents = new List<InventoryBase>();
                    }
                    return _contents;
                }
            }

            public bool TaskInvComplete
            {
                get
                {
                    return _taskInvComplete;
                }
                set
                {
                    _taskInvComplete = value;
                    SaveProgressFile();
                }
            }
            public bool Complete
            {
                get
                {
                    if (TaskInvComplete) return true;
                    if (TaskItemsToCreate == null) return false;
                    foreach (var itemTask in TaskItemsToCreate)
                    {
                        if (!itemTask.FindTaskItem())
                        {
                            return false;
                        }
                    }
                    TaskInvComplete = true;
                    return true;
                }
            }

            public List<TaskItemToCreate> TaskItemsToCreate;
            public event Action TaskInvChanged;
            public short TaskSerial = -1;
            private bool _taskInvComplete;

            private void TaskInventoryItemReceived(object sender, ObjectPropertiesEventArgs e)
            {
                if (e.Properties.ObjectID != NewID) return;
                short newSerial = e.Properties.InventorySerial;
                if (TaskSerial < newSerial)
                {
                    objectinventory = null;
                    if (TaskInvChanged != null)
                    {
                        TaskInvChanged();
                    }
                }
                TaskSerial = newSerial;
            }

            public void CreateWorkflow(UUID agentSyncFolderHolder)
            {
                if (TaskItemsToCreate != null) return;
                TaskItemsToCreate = new List<TaskItemToCreate>();
                if (IsLocalScene) return;
                AgentSyncFolder = ExportCommand.Running.FolderCalled(OldID.ToString(), agentSyncFolderHolder);
                Client.Objects.ObjectProperties += TaskInventoryItemReceived;
            }

            public bool LoadOSD(string taskDataS, OutputDelegate WriteLine)
            {
                failed = 0;
                succeeded = 0;
                var taskData = OSDParser.DeserializeLLSDXml(taskDataS) as OSDArray;
                if (taskData == null)
                {
                    WriteLine("ERROR: Cant read taskData: " + taskDataS);
                    return false;
                }

                lock (TaskItemsToCreate) if (taskData.Count == TaskItemsToCreate.Count)
                {
                    succeeded = taskData.Count;
                    return true;
                }
                // scan for existing source nodes
                lock (TaskItemsToCreate) foreach (OSDMap item in taskData)
                {
                    TaskItemsToCreate.Add(new TaskItemToCreate(this, item));
                    succeeded++;
                }
                return failed == 0;
            }

            public bool SyncToAgentFolder(OutputDelegate WriteLine, bool createObjects)
            {
                failed = 0;
                succeeded = 0;
                // create missing source nodes
                lock (TaskItemsToCreate) foreach (var itemTask in TaskItemsToCreate)
                {
                    if (!itemTask.CreateAgentItem(WriteLine, createObjects))
                    {
                        failed++;
                        TaskInvComplete = false;
                    }
                    else
                    {
                        succeeded++;
                    }
                }
                return failed == 0;
            }

            public bool SyncToObject(OutputDelegate WriteLine, bool createObjects)
            {
                failed = 0;
                bool invComplete = true;

                lock (TaskItemsToCreate) foreach (var itemTask in TaskItemsToCreate)
                {
                    if (!itemTask.FindAgentItem())
                    {
                        WriteLine("FAILED FindAgentItem: " + itemTask);
                        failed++;
                        invComplete = false;
                        continue;
                    }
                    if (!itemTask.CreateTaskItem(WriteLine, createObjects))
                    {
                        WriteLine("FAILED CreateTaskItem: " + itemTask);
                        invComplete = false;
                        failed++;
                        continue;
                    }
                    succeeded++;
                }
                if (invComplete) TaskInvComplete = true;
                return failed == 0;
            }

            public bool IsAsset = false;
            public void SetIsAsset()
            {
                IsAsset = true;
                if (!IsLinkParent) return;
                foreach (PrimToCreate c in Childs)
                {
                    c.SetIsAsset();
                }
            }
        }

        public class TaskItemToCreate
        {
            public override string ToString()
            {
                return AssetType + " " + OldAssetID + "  " + ItemName + "@" + CreatedPrim;
            }
            public readonly string ItemName;
            private UUID NewAgentItemID;
            public readonly OSDMap TaskOSD;
            public InventoryItem Item;
            public InventoryItem TaskItem;
            private readonly PrimToCreate CreatedPrim;
            public readonly AssetType AssetType;
            public bool AlreadyMovedToTask = false;
            public UUID OldAssetID;
            private readonly UUID OldItemID;

            protected InventoryManager Inventory
            {
                get { return CreatedPrim.Inventory; }
            }

            public TaskItemToCreate(PrimToCreate ptc, OSDMap taskOSD)
            {
                CreatedPrim = ptc;
                TaskOSD = taskOSD;
                ItemName = taskOSD["Name"];
                AssetType = (AssetType)taskOSD["AssetType"].AsInteger();
                OldAssetID = taskOSD["AssetUUID"].AsUUID();
                OldItemID = taskOSD["UUID"].AsUUID();
            }
            public bool FindAgentItem()
            {
                if (AlreadyMovedToTask) return true;
                if (IsLocalScene)
                {
                    NewAssetID = OldAssetID;                    
                    return true;
                }
                if (Item != null) return true;
                foreach (InventoryBase content in CreatedPrim.AgentContents)
                {
                    if (content.Name == ItemName)
                    {
                        Item = content as InventoryItem;
                        if (Item != null) return true;
                    }
                }
                return false;
            }
            public bool FindTaskItem()
            {
                if (TaskItem != null) return true;
                List<InventoryBase> taskInv = CreatedPrim.TaskInventory;
                if (taskInv == null) return false;
                foreach (InventoryBase content in taskInv)
                {
                    if (content.Name == ItemName)
                    {
                        var item = content as InventoryItem;
                        if (item == null) continue;
                        if (item.AssetType != AssetType) continue;
                        TaskItem = item;
                    }
                }
                return false;
            }


            private ManualResetEvent areItem;
            private short oldTaskSerial;
            private UUID NewAssetID;

            public bool CreateAgentItem(OutputDelegate WriteLine, bool createObjects)
            {
                if (FindAgentItem()) return true;
                if (IsLocalScene)
                {
                    NewAssetID = OldAssetID;
                    return true;
                }
                if (AssetType == AssetType.Object)
                {
                    string taskFileName = ExportCommand.dumpDir + OldItemID + ".taskobj";
                    if (!File.Exists(taskFileName))
                    {
                        WriteLine("ERROR: Cant restore Object asset: " + ToString());
                        return false;
                    }
                    if (!createObjects)
                    {
                        WriteLine("Skipping for now Object asset: " + ToString());
                        return true;
                    }
                    else
                    {
                        string taskFileContents = File.ReadAllText(taskFileName);
                        PrimToCreate innerObject = FindObject(taskFileContents, WriteLine);
                        if (innerObject.PackedInsideNow) return true;
                        if (innerObject.Complete)
                        {
                            // null for a refresh
                            CreatedPrim._contents = null;
                            // Remove World Object and PUT in AgentInvenory
                            areItem = areItem ?? new ManualResetEvent(false);
                            areItem.Reset();
                            Inventory.ItemReceived += AgentInventoryOnItemReceived;
                            Inventory.RequestDeRezToInventory(innerObject.NewLocalID, DeRezDestination.AgentInventoryTake,
                                                                     CreatedPrim.AgentSyncFolder, OldItemID);
                            if (!areItem.WaitOne(TimeSpan.FromSeconds(5)))
                            {
                                Inventory.ItemReceived -= AgentInventoryOnItemReceived;
                                WriteLine("ERROR: Cant derez inventory object: " + ToString());
                                return false;                                
                            }
                            Inventory.ItemReceived -= AgentInventoryOnItemReceived;
                            SetItemFromOSD(Item, true);
                            this.Inventory.RequestUpdateItem(Item);
                            innerObject.PackedInsideNow = true;
                            innerObject.SaveProgressFile();
                            return FindAgentItem();
                        } else
                        {
                            WriteLine("ERROR: Inner object not ready: " + innerObject);
                        }
                        return false;
                    }

                }

                UUID NewItemID;
                if (CogbotHelpers.IsNullOrZero(OldAssetID))
                {
                    WriteLine("FAILED: NO AssetID: " + ToString());
                    //skipped++;
                    var scriptsUUID = ExportCommand.Running.FolderCalled("Scripts");
                    InventoryItem missingItem = ExportCommand.Running.GetInvItem(Client, "MissingItemScript", scriptsUUID);
                    NewItemID = missingItem.UUID;
                    NewAssetID = missingItem.AssetUUID;
                }
                else
                {
                    ItemToCreate itc = Running.FindItemToCreate(OldAssetID, AssetType, false);
                    NewItemID = itc.NewItemID;
                }
                areItem = areItem ?? new ManualResetEvent(false);
                areItem.Reset();
                //Inventory.ItemReceived += AgentInventoryOnItemReceived;
                Inventory.RequestCopyItem(NewItemID, CreatedPrim.AgentSyncFolder, ItemName, AgentInventoryOnItemReceived);
                if (!areItem.WaitOne(TimeSpan.FromSeconds(5)))
                {
                    //Inventory.ItemReceived -= AgentInventoryOnItemReceived;
                    WriteLine("FAILED: Cant copy inventory asset: " + this);
                    return false;
                }
                //Inventory.ItemReceived -= AgentInventoryOnItemReceived;
                NewAssetID = Item.AssetUUID;
                SetItemFromOSD(Item, true);
                this.Inventory.RequestUpdateItem(Item);
                return true;
            }

            private void AgentInventoryOnItemReceived(object sender, ItemReceivedEventArgs e)
            {
                var item = e.Item;
                if (item.ParentUUID != CreatedPrim.AgentSyncFolder) return;
                if (item.Name != ItemName) return;
                AgentInventoryOnItemReceived(e.Item);
            }

            private void AgentInventoryOnItemReceived(InventoryBase item)
            {
                if (item.Name != ItemName) return;
                NewAgentItemID = item.UUID;
                Item = item as InventoryItem;
                areItem.Set();
            }

            public bool CreateTaskItem(OutputDelegate WriteLine, bool createObjects)
            {
                if (FindTaskItem()) return true;

                if (!FindAgentItem())
                {
                    WriteLine("NULL: FindAgentItem: " + ToString());
                    return false;
                }

                if (AssetType == AssetType.Object && !IsLocalScene)
                {
                    if (!createObjects) return true;
                }

                if (IsLocalScene)
                {
                    NewAssetID = OldAssetID;
                    lock (CreatedPrim.TaskItemsToCreate)
                        if (!CreatedPrim.TaskItemsToCreate.Contains(this))
                            CreatedPrim.TaskItemsToCreate.Add(this); 
                    return true;
                }
                // Copy to Task
                SetItemFromOSD(Item, true);
                areItem = areItem ?? new ManualResetEvent(false);
                areItem.Reset();
                oldTaskSerial = CreatedPrim.TaskSerial;
                if (oldTaskSerial == -1)
                {
                    SimObject createdPrimRezed = CreatedPrim.Rezed;
                    if (createdPrimRezed == null)
                    {
                        WriteLine("TIMEOUT: TaskSerial in " + ToString());
                    }
                    else
                    {
                        oldTaskSerial = createdPrimRezed.Properties.InventorySerial;
                    }
                }
                Client.Objects.ObjectProperties += TaskInventoryItemReceived;

                if (Item.InventoryType == InventoryType.LSL)
                {
                    Inventory.CopyScriptToTask(CreatedPrim.NewLocalID, (InventoryItem) Item, true);
                    Inventory.RequestSetScriptRunning(CreatedPrim.NewID, Item.AssetUUID, true);
                }
                else
                {
                    Inventory.UpdateTaskInventory(CreatedPrim.NewLocalID, (InventoryItem) Item);                   
                }
                if (!areItem.WaitOne(TimeSpan.FromSeconds(5)))
                {
                    WriteLine("TIMEOUT: UpdateTask in " + ToString());
                }
                Client.Objects.ObjectProperties -= TaskInventoryItemReceived;
                var revent = CreatedPrim.RequestNewTaskInventory();
                bool timedOut = false;
                if (!revent.WaitOne(TimeSpan.FromSeconds(10)))
                {
                    timedOut = true;
                }
                bool found = FindTaskItem();;
                if (AssetType == AssetType.Object)
                {
                    AlreadyMovedToTask = found;
                }
                if (!found)
                {
                    if (timedOut) WriteLine("TIMEOUT: RequestNewTaskInventory in " + ToString());
                }
                else
                {
                    WriteLine("SUCCESS: Found " + ToString());
                }
                return found;
            }

            private void TaskInventoryItemReceived(object sender, ObjectPropertiesEventArgs e)
            {
                if (e.Properties.ObjectID == CreatedPrim.NewID)
                {
                    if (oldTaskSerial < e.Properties.InventorySerial)
                    {
                        areItem.Set();
                    }
                }
            }

            private void SetItemFromOSD(InventoryItem item, bool restoreItemMeta)
            {
                var fid = item.ParentUUID;
                var iid = item.UUID;
                var aid = item.AssetUUID;
                var ast = item.AssetType;
                OSD.SetObjectOSD(item, TaskOSD);
                ReplaceAllMembers(item, typeof(UUID), UUIDReplacer, MissingFromExport);
                if (!restoreItemMeta) return;
                item.ParentUUID = fid;
                item.UUID = iid;
                item.AssetUUID = aid;
                item.AssetType = ast;
            }

            protected BotClient Client
            {
                get { return CreatedPrim.Client; }
            }

            public PrimToCreate FindObject(string contents, OutputDelegate WriteLine)
            {
                var splt = contents.Split(',');
                var oldID = UUID.Parse(splt[3]);
                return Running.GetOldPrim(oldID);
            }

            private InventoryItem _item;
            public InventoryItem ToInventoryBase()
            {
                if (_item == null)
                {
                    int pn;
                    _item = InventoryManager.CreateInventoryItem(
                        ItemToCreate.AssetTypeToInventoryType(this.AssetType, out pn), OldItemID);
                    if (_item == null)
                    {
                        _item = new InventoryItem(OldItemID);
                    }
                    SetItemFromOSD(_item, false);
                    if (CogbotHelpers.IsNullOrZero(OldAssetID))
                    {
                        if (_item.AssetType != AssetType.Object)
                        {
                            OldAssetID = GetMissingFiller(_item.AssetType);
                        }
                    }
                    _item.AssetUUID = OldAssetID;
                }
                return _item;
            }
        }
    }
}
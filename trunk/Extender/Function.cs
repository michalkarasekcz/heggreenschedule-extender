#define XMLComunicator

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

using Noris.Schedule.Support;
using Noris.Schedule.Support.Services;
using Noris.Schedule.Planning.ProcessData;
using Noris.Schedule.Planning.DataFace;

using Noris.WS.ServiceGate;

#if XMLComunicator
using XmlCommunicator;
#endif


namespace Noris.Schedule.Extender
{
    public class PlanCombin : IFunctionMenuItem
    {
        bool IFunctionMenuItem.IsFunctionSuitableFor(FunctionMenuItemSuitableArgs args)
        {
            bool result;

            result = (args.KeyGraphMode == RowGraphMode.TaskCapacityLink &&
                args.KeyAreaType == FunctionMenuItemAreaType.RowHeader &&
                args.KeyRowClassNumber == 0x4002);
            if (result)
            {
                args.MenuCaption = "Zaplánuj kombinaci";
                args.MenuToolTipText = "Zaplánuj kombinaci";
            }
            return result;            
        }

        bool IFunctionMenuItem.IsMenuItemEnabledFor(Noris.Schedule.Support.Services.FunctionMenuItemRunArgs args)
        {
            bool result;

            result = true;
            return result;
        }

        void IFunctionMenuItem.Run(FunctionMenuItemRunArgs args)
        {
            ExtenderDataSource data;
            int workplaceForChange;
            decimal qtyForChange;
            DateTime startTimeForChange;
            Dictionary<PressFactCombinDataCls, CapacityPlanWorkItemCls> combinItemsFirstWorkItem;
            List<DataPointerStr> splitElements;
            List<int> splitWorkItemIDs;
            data = (ExtenderDataSource)args.DataSource;
            combinItemsFirstWorkItem = _GetCombinItemsFirstWorkItem(data, args.ClickedItem.Row.RecordNumber);
            if (_SetParams(data, combinItemsFirstWorkItem, out qtyForChange, out startTimeForChange, out workplaceForChange))
            {
                splitElements = _Split(data, combinItemsFirstWorkItem, qtyForChange, ref args);
                splitWorkItemIDs = _MoveUnitAndTime(data, splitElements, workplaceForChange, startTimeForChange, ref args);
                _CreateLink(data, splitWorkItemIDs, args.ClickedItem.Row.RecordNumber);
                _RunPlanningRePlanUnfixedToHistory(data);
                _Refresh(data, splitElements, args);
                MessageBox.Show("Úspěšné ukončení funkce.");
            }
        }

        private bool _SetParams(ExtenderDataSource data, Dictionary<PressFactCombinDataCls, CapacityPlanWorkItemCls> combinItemsFirstWorkItem, out decimal qty, out DateTime startTime, out int workplace)
        {
            bool result = true;
            ZaplanujKombinaci paramsForm;
            List<KeyValuePair<int, string>> workplaceList;

            qty = _GetMinQty(combinItemsFirstWorkItem);
            workplaceList = _GetWorkplaceList(data, combinItemsFirstWorkItem);
            paramsForm = new ZaplanujKombinaci(data, combinItemsFirstWorkItem, qty, workplaceList);
            paramsForm.ShowDialog();

            result = paramsForm.OK;
            qty = paramsForm.Qty;
            startTime = paramsForm.StartTime;
            workplace = paramsForm.Workplace;
            if (result && workplace == 0)
                Throw.BreakError("Pracoviště musí být zadané.");

            return result;
        }

        /// <summary>
        /// Pro kazdou položku konkretni kombinace vylisku vraci prvni vyrobni operaci
        /// </summary>
        /// <param name="data"></param>
        /// <param name="pressFactCombin">daná Konkrétní kombinace výlisků</param>
        /// <returns></returns>
        private Dictionary<PressFactCombinDataCls, CapacityPlanWorkItemCls> _GetCombinItemsFirstWorkItem(ExtenderDataSource data, int pressFactCombin)
        {
            Dictionary<PressFactCombinDataCls, CapacityPlanWorkItemCls> result;
            List<PressFactCombinDataCls> combinItems;
            IEnumerable<KeyValuePair<int, MaterialPlanAxisItemCls>> axises;
            List<KeyValuePair<int, CapacityPlanWorkItemCls>> workItems;
            KeyValuePair<int, CapacityPlanWorkItemCls> workItem;

            result = new Dictionary<PressFactCombinDataCls, CapacityPlanWorkItemCls>();
            // vyberu vsechny polozky konkretnich kombinaci vylisku pro jednu kombinaci
            combinItems = PressFactCombinDataCls.GetPressFactCombin(data.CombinData, pressFactCombin);
            /*
             pro kazdou polozku hledam polozky materialu z osy S, ktere maji dilec VTPV (vyvojove TPV) stejne jako je dilec na polozce konkretni polozce
             a datum na ose S NENI FIXNI
            */
            foreach (PressFactCombinDataCls combinItem in combinItems)
            {
                axises = data.PlanningProcess.DataAxisS.Where(axis =>
                    axis.Value.ConstrElement == combinItem.ConstrElementItem
                    && !axis.Value.IsFixedAxis);               
                workItems = new List<KeyValuePair<int, CapacityPlanWorkItemCls>>();
                foreach (KeyValuePair<int, MaterialPlanAxisItemCls> axis in axises)
                {
                    workItem = _GetFirstWorkItem(data, axis.Key);
                    if (workItem.Key > 0)                    
                        workItems.Add(workItem);                                   
                }
                workItems.Sort(_CompareWorkItemStart);
                if (workItems.Count > 0)
                    result.Add(combinItem, workItems[0].Value);
                else
                    Throw.BreakError("Pro dílec " + combinItem.CEItemRefer + " na kombinaci výlisků " + combinItem.Reference + " nebyl nalezen kapacitní úkol!");                     
            }
            return result;
        }

        /// <summary>
        /// Vrací defaultní počáteční čas pro zaplánování úkolů.
        /// a) Zjistí datum konce poslední zafixované operace pro dané pracoviště. 
        /// b) Zjistí datum počátku první nezafixované operace pro dané pracoviště dané konkrétní kombinace.
        /// Vrátí větší z obou časů.
        /// Pokud neexistují ani nezafixované ani nezafixované úkoly, vrací minimální čas.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="combinItemsFirstWorkItem"></param>
        /// <param name="workplace"></param>
        /// <returns></returns>
        public static DateTime GetStartTime(ExtenderDataSource data, Dictionary<PressFactCombinDataCls, CapacityPlanWorkItemCls> combinItemsFirstWorkItem, int workplace)
        {
            DateTime result, maxFixWorkItemTime, minCombinWorkItemTime;
            KeyValuePair<int, CapacityPlanWorkItemCls> workItem;

            //a) konec posledniho fixovaneho ukolu KPJ daneho pracoviste
            workItem = _GetLastFixWorkItem(data, workplace);
            maxFixWorkItemTime = (workItem.Key > 0) ? workItem.Value.TimeWork.End : new DateTime(1900, 1, 1);

            //b) zacatek prvniho nefixovaneho ukolu vybrane konkretni kombinace
            minCombinWorkItemTime = _GetMinDateTime(combinItemsFirstWorkItem);

            result = (maxFixWorkItemTime > minCombinWorkItemTime) ? maxFixWorkItemTime : minCombinWorkItemTime;
            return result;
        }

        /// <summary>
        /// Vrací první operaci (kapacitni ukol) daného záznamu osy, která má navázanou některou KPJ z LisovnaUnits (= KPJ se zdrojem = pracoviště Lisovna).
        /// Pokud žádná neexistuje, vrací prázdný záznam.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="axis"></param>
        /// <returns></returns>
        private static KeyValuePair<int, CapacityPlanWorkItemCls> _GetFirstWorkItem(ExtenderDataSource data, int axis)
        {
            KeyValuePair<int, CapacityPlanWorkItemCls> result;
            IEnumerable<KeyValuePair<int, CapacityPlanWorkItemCls>> workItems;      //vsechny operace osy
            List<KeyValuePair<int, CapacityPlanWorkItemCls>> workItemsWithUnit;     //operace osy s pracovistem Lisovna
            List<KeyValuePair<int, decimal>> units;
            workItemsWithUnit = new List<KeyValuePair<int, CapacityPlanWorkItemCls>>();
            workItems = data.PlanningProcess.DataTaskC.Where(w => w.Value.AxisID == axis && !w.Value.IsFixedTask);
            foreach (KeyValuePair<int, CapacityPlanWorkItemCls> workItem in workItems)
            {
                units = workItem.Value.GetAllPlanUnitCCapacityList();
                foreach (KeyValuePair<int, decimal> unit in units)
                    if (data.LisovnaUnits.Contains(unit.Key))
                        workItemsWithUnit.Add(new KeyValuePair<int, CapacityPlanWorkItemCls>(workItem.Key, workItem.Value));
            }
            if (workItemsWithUnit.Count > 0)
            {
                workItemsWithUnit.Sort(_CompareWorkItemStart);
                result = workItemsWithUnit[0];
            }
            else
                result = new KeyValuePair<int, CapacityPlanWorkItemCls>();
            return result;
        }

        /// <summary>
        /// Vrací poslední operaci libovolného fixovaného úkolu, která má navázanou právě jednu KPJ na dané pracoviště.
        /// Pokud žádná neexistuje, vrací prázdný záznam.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="workplace"></param>
        /// <returns></returns>
        private static KeyValuePair<int, CapacityPlanWorkItemCls> _GetLastFixWorkItem(ExtenderDataSource data, int workplace)
        {
            KeyValuePair<int, CapacityPlanWorkItemCls> result;
            IEnumerable<KeyValuePair<int, CapacityPlanWorkItemCls>> workItems;
            List<KeyValuePair<int, CapacityPlanWorkItemCls>> workItemsWithUnit;
            List<KeyValuePair<int, decimal>> units;
            CapacityUnitCls workUnit;

            workItemsWithUnit = new List<KeyValuePair<int, CapacityPlanWorkItemCls>>();
            workUnit = _GetUnit(data, workplace);
            workItems = data.PlanningProcess.DataTaskC.Where(w => w.Value.IsFixedTask);
            foreach (KeyValuePair<int, CapacityPlanWorkItemCls> workItem in workItems)
            {
                units = workItem.Value.GetAllPlanUnitCCapacityList();
                foreach (KeyValuePair<int, decimal> unit in units)
                    if (unit.Key == workUnit.PlanUnitC)
                        workItemsWithUnit.Add(new KeyValuePair<int, CapacityPlanWorkItemCls>(workItem.Key, workItem.Value));
            }
            if (workItemsWithUnit.Count > 0)
            {
                workItemsWithUnit.Sort(_CompareWorkItemEnd);
                result = workItemsWithUnit[workItemsWithUnit.Count - 1];
            }
            else
                result = new KeyValuePair<int, CapacityPlanWorkItemCls>();
            return result;
        }

        private static int _CompareWorkItemStart(KeyValuePair<int, CapacityPlanWorkItemCls> workItem1, KeyValuePair<int, CapacityPlanWorkItemCls> workItem2)
        {
            int result;
            result = workItem1.Value.TimeWork.Begin.CompareTo(workItem2.Value.TimeWork.Begin);
            return result;
        }

        private static int _CompareWorkItemEnd(KeyValuePair<int, CapacityPlanWorkItemCls> workItem1, KeyValuePair<int, CapacityPlanWorkItemCls> workItem2)
        {
            int result;
            result = workItem1.Value.TimeWork.End.CompareTo(workItem2.Value.TimeWork.End);
            return result;
        }

        /// <summary>
        /// Vrací nejmenší požadované množství ze všech prvních operací všech nezafixovaných záznamů osy, 
        /// které jsou na některý dílec ze všech položek vybrané konkrétní kombinace.
        /// Pokud neexistuje žádná operace, vrací nulu.
        /// </summary>
        /// <param name="combinItemsFirstWorkItem"></param>
        /// <returns></returns>
        private static decimal _GetMinQty(Dictionary<PressFactCombinDataCls, CapacityPlanWorkItemCls> combinItemsFirstWorkItem)
        {
            decimal result, qty;
            Dictionary<CapacityPlanWorkItemCls, int> workItemsParalel;

            result = 0;
            workItemsParalel = _GetWorkItemsParalel(combinItemsFirstWorkItem);
            foreach (KeyValuePair<CapacityPlanWorkItemCls, int> workItemParalel in workItemsParalel)
            {
                qty = Math.Round(workItemParalel.Key.QtyRequired / workItemParalel.Value, 2);
                if (result == 0 || result > qty)
                    result = qty;
            }

            return result;
        }

        /// <summary>
        /// Vrací čas počátku první operace ze všech nezafixovaných záznamů osy, 
        /// které jsou na některý dílec ze všech položek vybrané konkrétní kombinace.
        /// Pokud neexistuje žádná operace, vrací minimální čas.
        /// </summary>
        /// <param name="combinItemsFirstWorkItem"></param>
        /// <returns></returns>
        private static DateTime _GetMinDateTime(Dictionary<PressFactCombinDataCls, CapacityPlanWorkItemCls> combinItemsFirstWorkItem)
        {
            DateTime result;

            result = DateTime.MinValue;
            foreach (KeyValuePair<PressFactCombinDataCls, CapacityPlanWorkItemCls> c in combinItemsFirstWorkItem)
                if (c.Value != null && (result == DateTime.MinValue || result > c.Value.StartTime))
                    result = c.Value.TimeWork.Begin;

            return result;
        }

        /// <summary>
        /// Vrácí seznam všech pracovišť dané Konkrétní kombinace výlisků (DV 22919).
        /// Pokud neexistuje ani jedno pracoviště, vrací prázdný seznam.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="pressFactCombin"></param>
        /// <returns></returns>
        private static List<KeyValuePair<int, string>> _GetWorkplaceList(ExtenderDataSource data, Dictionary<PressFactCombinDataCls, CapacityPlanWorkItemCls> combinItemsFirstWorkItem)
        {
            List<KeyValuePair<int, string>> result;

            result = new List<KeyValuePair<int, string>>();
            foreach (KeyValuePair<PressFactCombinDataCls, CapacityPlanWorkItemCls> c in combinItemsFirstWorkItem)
                foreach (KeyValuePair<int, string> wokplace in c.Key.Workplaces)
                    if (!result.Contains(wokplace))
                        result.Add(wokplace);
            return result;
        }

        /// <summary>
        /// Vrací KPJ, která má zdroj shodný s daným pracovištěm.
        /// Pokud neexistuje právě jedna KPJ, vyvolá výjimku.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="workplace"></param>
        /// <returns></returns>
        private static CapacityUnitCls _GetUnit(ExtenderDataSource data, int workplace)
        {
            CapacityUnitCls result = null;
            List<PlanCSourceLinkCls> links;          
            links = data.PlanningProcess.CapacityData.FindLinksToCapacityUnitForSource(workplace);
            if (links.Count() != 1)
                Throw.BreakError("K pracovišti " + workplace.ToString() + " se jednoznačně nedohledala kapacitní plánovací jednotka.");
            else
                data.PlanningProcess.DataCapacityUnit.TryGetValue(links[0].PlanUnitC, out result);

            return result;
        }

        private List<DataPointerStr> _Split(ExtenderDataSource data, Dictionary<PressFactCombinDataCls, CapacityPlanWorkItemCls> combinItemsFirstWorkItem, decimal qty, ref FunctionMenuItemRunArgs args)
        {
            List<DataPointerStr> result;
            Dictionary<CapacityPlanWorkItemCls, int> workItemsParalel;

            result = new List<DataPointerStr>();
            workItemsParalel = _GetWorkItemsParalel(combinItemsFirstWorkItem);
            foreach (KeyValuePair<CapacityPlanWorkItemCls, int> workItemParalel in workItemsParalel)
            {
                if (workItemParalel.Value == 1)
                    result.AddRange(_SplitAxis(data, workItemParalel, qty, ref args));
                else
                {
                    KeyValuePair<CapacityPlanWorkItemCls, int> wip;
                    decimal qty1;

                    qty1 = qty * workItemParalel.Value;
                    wip = workItemParalel;
                    if (workItemParalel.Key.QtyRequired != qty1)
                    {
                        List<DataPointerStr> pom;
                        WorkUnitCls workUnit;
                        CapacityPlanWorkItemCls workItem;

                        pom = _SplitAxis(data, workItemParalel, qty1, ref args);
                        if (pom.Count > 0)
                        {
                            workUnit = data.PlanningProcess.AxisHeap.FindIWorkItem(pom[0].Element.RecordNumber);
                            workItem = data.PlanningProcess.AxisHeap.FindTaskCItem(workUnit.TaskID);
                            wip = new KeyValuePair<CapacityPlanWorkItemCls, int>(workItem, workItemParalel.Value);
                        }
                    }
                    result.AddRange(_SplitTaskParalel(data, wip, qty, ref args));
                }
            }
            return result;
        }

        private List<DataPointerStr> _SplitAxis(ExtenderDataSource data, KeyValuePair<CapacityPlanWorkItemCls, int> workItemParalel, decimal qty, ref FunctionMenuItemRunArgs args)
        {
            List<DataPointerStr> result;
            List<KeyValuePair<int, WorkUnitCls>> workUnits;

            result = new List<DataPointerStr>();
            workUnits = _GetWorkUnits(workItemParalel.Key);
            if (workUnits.Count > 0)
            {
                if (workItemParalel.Key.QtyRequired != qty)
                {
                    PlanningInteractiveSplitAxisSArgs splitArgs;
                    PlanningInteractiveSplitAxisSItemArgs splitArgPom;
                    decimal qty1;

                    workUnits.Sort(_CompareWorkUnitBegin);
                    splitArgs = new PlanningInteractiveSplitAxisSArgs();
                    splitArgs.SplitItemSource = workUnits[0].Value.DataPointer;
                    splitArgs.SplitItemList.Add(new PlanningInteractiveSplitAxisSItemArgs(qty, Noris.Schedule.Support.TimeRange.TimeDirection.ToFuture, 1));
                    qty1 = Math.Max(0, workItemParalel.Key.QtyRequired - qty);
                    if (qty1 > 0)
                        splitArgs.SplitItemList.Add(new PlanningInteractiveSplitAxisSItemArgs(qty1, Noris.Schedule.Support.TimeRange.TimeDirection.ToFuture, 0));

                    data.PlanningProcess.PlanningData.InteractiveSplitAxisS(splitArgs);
                    splitArgs.ChangedRowsCopyTo(args);
                    splitArgPom = splitArgs.SplitItemList.Find(a => (int)a.Tag == 1);
                    result = splitArgPom.ResultElementPointerList;
                }
                else
                    result.Add(workUnits[0].Value.DataPointer);
            }
            return result;
        }

        private List<DataPointerStr> _SplitTaskParalel(ExtenderDataSource data, KeyValuePair<CapacityPlanWorkItemCls, int> workItemParalel, decimal qty, ref FunctionMenuItemRunArgs args)
        {
            List<DataPointerStr> result;
            List<KeyValuePair<int, WorkUnitCls>> workUnits;
            PlanningInteractiveSplitTaskCArgs splitTaskArgs;

            result = new List<DataPointerStr>();
            workUnits = _GetWorkUnits(workItemParalel.Key);
            if (workUnits.Count > 0)
            {
                workUnits.Sort(_CompareWorkUnitBegin);
                splitTaskArgs = new PlanningInteractiveSplitTaskCArgs();
                splitTaskArgs.SplitItemSource = workUnits[0].Value.DataPointer;
                for (int i = 1; i <= workItemParalel.Value; i++)
                    splitTaskArgs.SplitItemList.Add(new PlanningInteractiveSplitTaskCItemArgs(qty, new TimeVector(DateTime.MinValue, TimeRange.TimeDirection.ToFuture), i));

                data.PlanningProcess.PlanningData.InteractiveSplitTaskC(splitTaskArgs);
                foreach (PlanningInteractiveSplitTaskCItemArgs splitTaskArg in splitTaskArgs.SplitItemList)
                    result.AddRange(splitTaskArg.ResultElementPointerList);
                splitTaskArgs.ChangedRowsCopyTo(args);
            }
            return result;
        }

        /// <summary>
        /// Vrátí seznam dvojic: první nefixovaná operace na daný dílec položky kombinace - počet výskytů této operace v položkách kombinace.
        /// </summary>
        /// <param name="combinItemsFirstWorkItem"></param>
        /// <returns></returns>
        private static Dictionary<CapacityPlanWorkItemCls, int> _GetWorkItemsParalel(Dictionary<PressFactCombinDataCls, CapacityPlanWorkItemCls> combinItemsFirstWorkItem)
        {
            Dictionary<CapacityPlanWorkItemCls, int> result;
            IEnumerable<KeyValuePair<PressFactCombinDataCls, CapacityPlanWorkItemCls>> pom;

            result = new Dictionary<CapacityPlanWorkItemCls, int>();
            foreach (KeyValuePair<PressFactCombinDataCls, CapacityPlanWorkItemCls> c in combinItemsFirstWorkItem)
            {
                pom = combinItemsFirstWorkItem.Where(cPom => cPom.Value != null && cPom.Value.TaskID == c.Value.TaskID);
                if (c.Value != null && !result.Keys.Contains(c.Value))
                    result.Add(c.Value, pom.Count());
            }
            return result;
        }
        /// <summary>
        /// Přeplánuje kapactni ukol podle zadaneho času a pracoviste, tyto ukoly na casove ose a pracovisti zafixuje
        /// </summary>
        /// <param name="data"></param>
        /// <param name="splitElements"></param>
        /// <param name="workplace"></param>
        /// <param name="time"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        private List<int> _MoveUnitAndTime(ExtenderDataSource data, List<DataPointerStr> splitElements, int workplace, DateTime time, ref FunctionMenuItemRunArgs args)
        {
            List<int> result;
            PlanningInteractiveMoveArgs moveArgs;
            PlanningInteractiveMoveActiveItem activeItem;
            //PlanningVisualDataElementCls element;
            WorkUnitCls workUnit;
            CapacityUnitCls unit;

            result = new List<int>();
            moveArgs = new PlanningInteractiveMoveArgs();
            moveArgs.CapacityLimitForActiveTask = LimitedCType.Unlimited;
            moveArgs.SetFixedTask = true;
            moveArgs.PullAdjacentForActiveTask = true;
            moveArgs.PullAdjacentForActiveTree = true;
            unit = _GetUnit(data, workplace);            
            
            foreach (DataPointerStr splitElement in splitElements)
            {
                workUnit = data.PlanningProcess.AxisHeap.FindIWorkItem(splitElement.Element);
                if (!result.Contains(workUnit.TaskID))
                    result.Add(workUnit.TaskID);
                //element = ExtenderDataSource.GetElementsWorkUnit(splitElement.Row, workUnit);              
                //element.RowGId = new GID(PlanUnitCCls.ClassNr, unit.PlanUnitC);                
                //element.TimeRange = new TimeRange(time, time.Add(element.TimeRange.End - element.TimeRange.Begin));               
                //activeItem = new PlanningInteractiveMoveActiveItem(element);
                //activeItem.CapacitySourceCurrent = workplace;  // defince pacoviste, na ktere zaplanovat kapacitni ukol 
                TimeRange timeRange = workUnit.WorkTime.GetMovedToBegin(time);
                activeItem = new PlanningInteractiveMoveActiveItem(workUnit, timeRange, unit.PlanUnitC, workplace);
                //activeItem = new PlanningInteractiveMoveActiveItem(workUnit, timeRange, workplace, unit.PlanUnitC);
                moveArgs.AddActiveItem(activeItem);
            }            
            data.PlanningProcess.PlanningData.InteractiveMove(moveArgs);
            moveArgs.ChangedRowsCopyTo(args);

            return result;
        }

        private void _CreateLink(ExtenderDataSource data, List<int> splitWorkItemIDs, int pressFactCombin)
        {
            LinkCls link;
            CapacityPlanWorkItemCls workItem;
            List<PressFactCombinDataCls> pfcs;

            link = new LinkCls(true);
            link.PressFactCombin = pressFactCombin;
            link.FolderNumber = 22228;
            pfcs = PressFactCombinDataCls.GetPressFactCombin(data.CombinData, pressFactCombin);
            if (pfcs.Count > 0)
            {
                link.Reference = pfcs[0].Reference;
                link.Nazev = pfcs[0].Nazev;
            }
            foreach (int workItemID in splitWorkItemIDs)
            {
                workItem = data.PlanningProcess.AxisHeap.FindTaskCItem(workItemID);
                workItem.LinkObject = link;
            }
        }

        private void _RunPlanningRePlanUnfixedToHistory(ExtenderDataSource data)
        {
            PlanningInteractiveRePlanArgs args = new PlanningInteractiveRePlanArgs();
            args.CapacityLimit = LimitedCType.ByPUCsetting;
            args.RegisterUnfixedTimeDir = TimeRange.TimeDirection.ToHistory;
            data.PlanningProcess.PlanningData.InteractivePlanningRePlan(args);
        }

        private static int _CompareWorkUnitBegin(KeyValuePair<int, WorkUnitCls> workUnit1, KeyValuePair<int, WorkUnitCls> workUnit2)
        {
            int result;
            result = workUnit1.Value.WorkTime.Begin.CompareTo(workUnit2.Value.WorkTime.Begin);
            return result;
        }

        private void _Refresh(ExtenderDataSource data, List<DataPointerStr> splitElements, FunctionMenuItemRunArgs args)
        {
            GID gid;
            foreach (PressFactCombinDataCls combin in data.CombinData)
            {
                gid = new GID(0x4002, combin.CisloSubjektu);
                if (!args.ResultEditChangedRows.Contains(gid))
                    args.ResultEditChangedRows.Add(gid);
                args.ResultEditChangedRows.Add(new GID(22290, combin.CisloObjektu));
            }
            args.ResultEditChangedRows.Add(new GID(0x4001, 1));
            foreach (int lisovaUnit in data.LisovnaUnits)
                args.ResultEditChangedRows.Add(new GID(PlanUnitCCls.ClassNr, lisovaUnit));
        }

        private List<KeyValuePair<int, WorkUnitCls>> _GetWorkUnits(CapacityPlanWorkItemCls workItem)
        {
            List<KeyValuePair<int, WorkUnitCls>> result;

            result = new List<KeyValuePair<int, WorkUnitCls>>();
            foreach (WorkPassCls workPass in workItem.WorkPassList)
                foreach (WorkTimeCls workTime in workPass.WorkTimeList)
                    foreach (WorkUnitCls workUnit in workTime.WorkUnitList)
                        result.Add(new KeyValuePair<int, WorkUnitCls>(workUnit.WorkID, workUnit));
            return result;
        }
    }
   
    public class GeneratePO : IFunctionMenuItem
    {
        bool IFunctionMenuItem.IsFunctionSuitableFor(FunctionMenuItemSuitableArgs args)
        {
            bool result;

            result = (args.KeyGraphMode == RowGraphMode.TaskCapacityLink &&
                            args.KeyAreaType == FunctionMenuItemAreaType.GraphElement &&
                            args.KeyElementClassNumber == Noris.Schedule.Planning.ProcessData.Constants.ClassNumberWork);
            if (result)
            {
                args.MenuCaption = "Vystav VP z kapacitních úkolů";
                args.MenuToolTipText = "Vystav VP z kapacitních úkolů";
                args.EnabledStateDependingOnElement = true;
            }
            return result;
        }

        bool IFunctionMenuItem.IsMenuItemEnabledFor(Noris.Schedule.Support.Services.FunctionMenuItemRunArgs args)
        {
            bool result;
            ExtenderDataSource data;
            WorkUnitCls workUnitClicked;
            CapacityPlanWorkItemCls workItemClicked;

            if (args.DataSource is ExtenderDataSource)
            {
                data = (ExtenderDataSource)args.DataSource;
                workUnitClicked = data.PlanningProcess.AxisHeap.FindIWorkItem(args.ClickedItem.Element.RecordNumber);
                workItemClicked = data.PlanningProcess.AxisHeap.FindTaskCItem(workUnitClicked.TaskID);
                result = (!workItemClicked.LinkRecordNumber.IsNull);
            }
            else
                result = false;
            return result;
        }

        void IFunctionMenuItem.Run(FunctionMenuItemRunArgs args)
        {
            ExtenderDataSource data;
            List<int> axises;

            data = (ExtenderDataSource)args.DataSource;
            axises = _GetAxises(data, args.ClickedItem.Element.RecordNumber);
            if (_RunNorisFunction(data, PlanUnitSAxisCls.ClassNr, "IssueProductOrderFromTask", PlanUnitSAxisCls.ClassNr, axises))
                MessageBox.Show("Úspěšné ukončení funkce.");
            else
                MessageBox.Show("Neúspěšné ukončení funkce.");
        }

        private List<int> _GetAxises(ExtenderDataSource data, int workID)
        {
            List<int> result;
            WorkUnitCls workUnitClicked;
            CapacityPlanWorkItemCls workItemClicked;
            MaterialPlanAxisItemCls axis;
            IEnumerable<KeyValuePair<int, CapacityPlanWorkItemCls>> workItems;

            result = new List<int>();
            workUnitClicked = data.PlanningProcess.AxisHeap.FindIWorkItem(workID);
            workItemClicked = data.PlanningProcess.AxisHeap.FindTaskCItem(workUnitClicked.TaskID);
            if (workItemClicked.LinkRecordNumber <= 0)
                Throw.AplError(MessageInfo.Get("Zdrojová data nejsou uložena."));

            workItems = data.PlanningProcess.DataTaskC.Where(task => !task.Value.LinkRecordNumber.IsNull
                && task.Value.LinkRecordNumber.Value == workItemClicked.LinkRecordNumber.Value);
            foreach (KeyValuePair<int, CapacityPlanWorkItemCls> workItem in workItems)
            {
                axis = data.PlanningProcess.AxisHeap.FindAxisSItem(workItem.Value.AxisID);
                if (!result.Contains(axis.RecordNumber))
                    if (axis.RecordNumber > 0)
                        result.Add(axis.RecordNumber);
            }
            return result;
        }

        private bool _RunNorisFunction(ExtenderDataSource data, int dataFunctionClassNumber, string dataFunctionName, int recordNumbersClassNumber, List<int> recordNumbers)
        {
            bool result;
            // XmlConnector xc;
            RunFunctionRequest request;
            RunFunctionResponse response = null;
            string mess;

#if XMLComunicator
            string output;
            XmlConnector xc = new XmlConnector(ExtenderDataSource.ConnParam.ActiveUrl, "", "", "", ExtenderDataSource.ConnParam.ActiveUrl, ExtenderDataSource.ConnParam.SessionToken);
            request = new RunFunctionRequest();
            request.Function = new FunctionIdentification(dataFunctionClassNumber, dataFunctionName);
            request.Records.AddRange(recordNumbersClassNumber, recordNumbers);
            output = xc.ProcessXml(request.RawXml);
            response = (RunFunctionResponse)RunFunctionResponse.GetFromXml(output);
#else

            bool direct = false;
            try
            {
                if (direct)                
                    Steward.ServiceGateAdapter.RunFunction(recordNumbersClassNumber, dataFunctionName, recordNumbers);                                    
                else
                {
                    //ServiceGateConnector connect = ServiceGateConnector.Create(ExtenderDataSource.ConnParam.ActiveUrl);
                    //LogOnInfo logInfo = new LogOnInfo("", "", "");
                    ServiceGateConnector connect = ServiceGateConnector.Create(ExtenderDataSource.ConnParam.ActiveUrl);
                    string dbProfile = Steward.Connect.ProfileName;
                    string userName = Steward.CurrentUser.Login;
                    string password = Steward.NorisPasswordDecrypt("31dabf28f87a91ad41923fb1e761fcd32fa33a07c6d5692fd021b48043bedbead8ff88be38cce9ff253c648756d3adaafc062f27840227a4c7a9f5df5dc8dfaeecad7ae58b3a374cc6a8f632816fe48ce0be28835c066da14cc702a6d794f8cb04ba4d5e84295aa245fe47c7d85c7f7e211480fd687a484cc572f22427f7817d");
                    LogOnInfo logInfo = new LogOnInfo("", "", "");


                    using (var los = new LogOnScope(connect,logInfo))
                    {
                        request = new RunFunctionRequest();
                        request.Function = new FunctionIdentification(dataFunctionClassNumber, dataFunctionName);
                        request.Records.AddRange(recordNumbersClassNumber, recordNumbers);
                        response = (RunFunctionResponse)los.Connector.ProcessRequest(request);
                    }

                }
                result = true;
            }
            catch
            {
                result = false;
            }

#endif


            if (response.Auditlog == null)
                // mess = output;
                mess = response.RawXml;
            else
            {
                mess = String.Empty;
                if (response.Auditlog.Entries != null)
                    foreach (AuditlogEntry entry in response.Auditlog.Entries)
                        mess += entry.Message + "\r\n";
            }
            if (!string.IsNullOrEmpty(mess))
                MessageBox.Show(mess);
            result = (response.Auditlog.State != AuditlogState.Failure);

            return result;
        }
    }
}

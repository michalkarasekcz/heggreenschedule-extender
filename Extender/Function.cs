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
            List<DataPointerStr> splitElements;
            List<int> splitWorkItemIDs;
           
            data = (ExtenderDataSource)args.DataSource;
            // kolekce vsech polozek jedne kombinace konkretnich vylisku a prvni vyrobni operace pro tuto kombinaci
            Dictionary<PressFactCombinDataCls, CapacityPlanWorkItemCls> combinItemsFirstWorkItem = _GetCombinItemsFirstWorkItem(data, args.ClickedItem.Row.RecordNumber);
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

        private bool _SetParams(ExtenderDataSource data, Dictionary<PressFactCombinDataCls, CapacityPlanWorkItemCls> combinItemsFirstWorkItem, out decimal pocet_zalisu, out DateTime startTime, out int workplace)
        {
            bool result = true;
            ZaplanujKombinaci paramsForm;
            List<KeyValuePair<int, string>> workplaceList;

            pocet_zalisu = _GetMinQty(combinItemsFirstWorkItem);
            workplaceList = _GetWorkplaceList(data, combinItemsFirstWorkItem);
            paramsForm = new ZaplanujKombinaci(data, combinItemsFirstWorkItem, pocet_zalisu, workplaceList);
            paramsForm.ShowDialog();

            result = paramsForm.OK;
            pocet_zalisu = paramsForm.Pocet_zalisu;
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
            // kolekce konktretni kombinace a vyrobni operace      
            Dictionary<PressFactCombinDataCls, CapacityPlanWorkItemCls> result = new Dictionary<PressFactCombinDataCls, CapacityPlanWorkItemCls>();                                                           
            IEnumerable<KeyValuePair<int, MaterialPlanAxisItemCls>> axises;

            // V cyklu budu prochazet ty polozky kombinaci, jejich cislo_subjektu je rovno pressFactCombin z parametru
            foreach (PressFactCombinDataCls combinItem in data.CombinData.Where(item => item.CisloSubjektu == pressFactCombin))
            {
                List<KeyValuePair<int, CapacityPlanWorkItemCls>> workItems = new List<KeyValuePair<int, CapacityPlanWorkItemCls>>();
               // ze seznamu materialovych os S vyberu jen ty materialove OSY, ktere maji dilec VTPV (vyvojove TPV) stejne jako je dilec na polozce konkretni kombinace vylisku a datum na ose S NENI FIXNI
                axises = data.PlanningProcess.DataAxisS.Where(axis => axis.Value.ConstrElement == combinItem.ConstrElementItem && !axis.Value.IsFixedAxis);                
                foreach (KeyValuePair<int, MaterialPlanAxisItemCls> axis in axises)
                {
                    // z datoveho zdroje hledam prvni nefixovavnou vyrobni operaci pro danou materialovou osu ( axis key = identifikator materialove osy)
                    KeyValuePair<int, CapacityPlanWorkItemCls>  workItem = _GetFirstWorkItem(data, axis.Key); 
                    if (workItem.Key > 0)                    
                        workItems.Add(workItem);                                   
                }                
                if (workItems.Count > 0)
                {
                    workItems.Sort(_CompareWorkItemStart);
                    result.Add(combinItem, workItems[0].Value); // pro jednu polozku pridam prvni vyrobni operaci
                }
                else
                    Throw.BreakError("Pro dílec " + combinItem.CEItemRefer + " na kombinaci výlisků " + combinItem.Reference + " nebyl nalezen kapacitní úkol!");                     
            }
            return result;  // vratim kolekci  kombinace konkretniho vylisku a prvni vyrobni operace tohoto vylisku
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
        /// <param name="axis">identifikator materialove osy</param>
        /// <returns></returns>
        private static KeyValuePair<int, CapacityPlanWorkItemCls> _GetFirstWorkItem(ExtenderDataSource data, int axis)
        {
            KeyValuePair<int, CapacityPlanWorkItemCls> result;          
            List<KeyValuePair<int, decimal>> units;
            List<KeyValuePair<int, CapacityPlanWorkItemCls>> workItemsWithUnit = new List<KeyValuePair<int, CapacityPlanWorkItemCls>>(); // kolekce vsech operaci pro danou maetrialovou osu            
            
            // budu prochazet vsechny vyrobni operace (kapacitni ukoly), ktere nalezi na dane materialove ose dilce "axis" a nejsou fixovanne
            foreach (KeyValuePair<int, CapacityPlanWorkItemCls> workItem in data.PlanningProcess.DataTaskC.Where(w => w.Value.AxisID == axis && !w.Value.IsFixedTask))
            {
                units = workItem.Value.GetAllPlanUnitCCapacityList(); // pro kazdou vyrobni operace zjistim vsechny kapacitni jednotky, ktere se pro danou operaci vyuzivaji

                // zjistim  kolik z techto  kapacitnich jednotek ma zdroj na pracovisti Lisovna. Pokud existuje aspon jedna jednotka, pak pridam kapacitni ukol
                if (units.Where(item => data.LisovnaUnits.Contains(item.Key)).Count() > 0)
                    workItemsWithUnit.Add(workItem);
                                                                    
               
                /*   kod Jitky Tesarove
                foreach (KeyValuePair<int, decimal> unit in units)
                    if (data.LisovnaUnits.Contains(unit.Key))
                        workItemsWithUnit.Add(new KeyValuePair<int, CapacityPlanWorkItemCls>(workItem.Key, workItem.Value));
                 */
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
        /// Pro vybrane praocivste nalezne posledni fixovanou vyrobni operaci
        /// </summary>
        /// <param name="data"></param>
        /// <param name="workplace"></param>
        /// <returns></returns>
        private static KeyValuePair<int, CapacityPlanWorkItemCls> _GetLastFixWorkItem(ExtenderDataSource data, int workplace)
        {
            KeyValuePair<int, CapacityPlanWorkItemCls> result;                       
            List<KeyValuePair<int, decimal>> units;
            List<KeyValuePair<int, CapacityPlanWorkItemCls>> workItemsWithUnit = new List<KeyValuePair<int, CapacityPlanWorkItemCls>>();          
            CapacityUnitCls workUnit = _GetUnit(data, workplace);  // ziskam kapacitni jednotku pro dane pracoviste (workplace)
           
            // prochayi vsechny FIXOVANE vyrobni operace
            foreach (KeyValuePair<int, CapacityPlanWorkItemCls> workItem in data.PlanningProcess.DataTaskC.Where(w => w.Value.IsFixedTask))
            {
                // pro kazdy kapacitni ukol hledam kapacitni jednotky, ktere jsou vazany na stejne pracoviste (klic kapactnich jednotek = klic kapacitni jednotky dohledane podle pracoviste
                units = workItem.Value.GetAllPlanUnitCCapacityList();  // pro vyrobni operaci dohledma vsechny kapacitni jednotky

                if (units.Where(item => item.Key == workUnit.PlanUnitC).Count() > 0)
                    workItemsWithUnit.Add(workItem);
                
                //foreach (KeyValuePair<int, decimal> unit in units.Where(item => item.Key == workUnit.PlanUnitC)) // z vyrobni operace vyberu jen ty jeji kapacitni jednotky, jejich6 kli4 je stejnz jako klic kapactni jednotku pro dane pracoviste
                //{                   
                //    workItemsWithUnit.Add(new KeyValuePair<int, CapacityPlanWorkItemCls>(workItem.Key, workItem.Value));
                //}
            }
            if (workItemsWithUnit.Count > 0)
            {
                workItemsWithUnit.Sort(_CompareWorkItemEnd);                // setridim vybrane vyrobni operace podle casu konce
                result = workItemsWithUnit[workItemsWithUnit.Count - 1];    // vyberu posledni 
            }
            else
                result = new KeyValuePair<int, CapacityPlanWorkItemCls>();
            return result;
        }

        /// <summary>
        /// Setridi vyrobni operace podle casu zahajeni prace
        /// </summary>
        /// <param name="workItem1"></param>
        /// <param name="workItem2"></param>
        /// <returns></returns>
        private static int _CompareWorkItemStart(KeyValuePair<int, CapacityPlanWorkItemCls> workItem1, KeyValuePair<int, CapacityPlanWorkItemCls> workItem2)
        {
            int result;
            result = workItem1.Value.TimeWork.Begin.CompareTo(workItem2.Value.TimeWork.Begin);
            return result;
        }
        /// <summary>
        /// Setridi vyrobni operace podle casu KONCE vyrobni operace
        /// </summary>
        /// <param name="workItem1"></param>
        /// <param name="workItem2"></param>
        /// <returns></returns>
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
            decimal pocet_zdvihu = 0, qty;                        
            Dictionary<CapacityPlanWorkItemCls, decimal> workItemsParalel = _GetWorkItemsParalel(combinItemsFirstWorkItem);            
            // prochazim vsechny paralelni vyrobni operace
            foreach (KeyValuePair<CapacityPlanWorkItemCls, decimal> workItemParalel in workItemsParalel) // v decimal je pocet paralelnich operaci
            {
                qty = Math.Round(workItemParalel.Key.QtyRequired / workItemParalel.Value, 2);  // pocet zdvihu lisu, pri danem mnozstvi/ pocet parallenich pruchodu
                if (pocet_zdvihu == 0 || pocet_zdvihu > qty)
                    pocet_zdvihu = qty;
            }
            return pocet_zdvihu;
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
           DateTime result = DateTime.MinValue;
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
            List<KeyValuePair<int, string>> result = new List<KeyValuePair<int, string>>();
            foreach (KeyValuePair<PressFactCombinDataCls, CapacityPlanWorkItemCls> c in combinItemsFirstWorkItem)
                foreach (KeyValuePair<int, string> wokplace in c.Key.Workplaces)
                    if (!result.Contains(wokplace))
                        result.Add(wokplace);
            return result;
        }

        /// <summary>
        /// Pro obecný kapacitni zdroj(pracoviste) vratim prvni KONKRETNI kapacitni jednotku. 
        /// 
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

        /// <summary>
        /// Rozdělení jednotlivých paralelních operací na dílčí množství a na dílčí paralelní pruchody
        /// </summary>
        /// <param name="data"></param>
        /// <param name="combinItemsFirstWorkItem"></param>
        /// <param name="qty"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        private List<DataPointerStr> _Split(ExtenderDataSource data, Dictionary<PressFactCombinDataCls, CapacityPlanWorkItemCls> combinItemsFirstWorkItem, decimal zdvihy, ref FunctionMenuItemRunArgs args)
        {
            
            List<DataPointerStr> result = new List<DataPointerStr>();
            // Vyrobni operace a pocet jejich vyskytu na prvni pozici ze vsech polozek kombinaci vylisku
            Dictionary<CapacityPlanWorkItemCls, decimal> workItemsParalel = _GetWorkItemsParalel(combinItemsFirstWorkItem);
            
            // prochazim jednotlive vyrobni operace, ktere jsou zastoupeny v konkretni kombinaci vylisku
            foreach (KeyValuePair<CapacityPlanWorkItemCls, decimal> workItemParalel in workItemsParalel)  // 
            {                              
                decimal mnozstvi_zaplanovat_celkem = zdvihy * workItemParalel.Value; // celkove mnoztvi  = pocet zdvihu lisu * pocet paralelnich pruchodu. (na jeden zdvih se mohou vylisovat 2 stejne dilce)
                if (workItemParalel.Value == 1M) // vyrobni operace ma pouze jeden paralelni pruchod
                    result.AddRange(_SplitAxis(data, workItemParalel, mnozstvi_zaplanovat_celkem, ref args));
                else
                {   
                    // vyrobni operace ma vice paralelnich pruchodů                            
                    KeyValuePair<CapacityPlanWorkItemCls, decimal> wip = workItemParalel;
                    if (workItemParalel.Key.QtyRequired != mnozstvi_zaplanovat_celkem) // Pokud je celkove mnozstvi jine nez mnozstvi, ktere se ma zaplanovat, pak jej rozdelim na pozadovane mnozstvi a zbytkove
                    {                                                                   
                        List<DataPointerStr> pom = _SplitAxis(data, workItemParalel, mnozstvi_zaplanovat_celkem, ref args);
                        if (pom.Count > 0) // elememty,l ktere vznikly rozdelenim vyrobni operace na dilci mnozstvi
                        {
                            WorkUnitCls workUnit = data.PlanningProcess.PlanningData.FindWorkUnit(pom[0]); // pro prvni pointer naleznu pracovni jednotku (KPJ)
                            //WorkUnitCls workUnit = data.PlanningProcess.AxisHeap.FindIWorkItem(pom[0].Element.RecordNumber);    // vysledky planovaciho procesu = jednotka prace
                            CapacityPlanWorkItemCls workItem = data.PlanningProcess.AxisHeap.FindTaskCItem(workUnit.TaskID);      // pro kapacitni jednotku najdu prislusnou vyrobni operaci (ulohu)                           
                            wip = new KeyValuePair<CapacityPlanWorkItemCls, decimal>(workItem, workItemParalel.Value);  // 
                        }
                    }
                    // ve wip je vyrobni uloha, jiz upravena na pozdaovane mnozstvi ktere se ma zaplanovat
                    result.AddRange(_SplitTaskParalel(data, wip, zdvihy, ref args)); // pokud mam paralelni pruchod (na jeden zdvih lisu vypadnou 2 stejne dilce) pak toto mnozstvi musim toto mnozstvi rozhodit to tchto paralelnich pruchodu
                }
            }
            return result;
        }
        /// <summary>
        /// Rozdělení materiálové osy S na více dílčích přijmů s definovaným množstvím 
        /// Vrací seznam pointeru na elementy, ktere vznikly v průbehu rozdělení výroby pro zadané množství v parametru qty
        /// </summary>
        /// <param name="data"></param>
        /// <param name="workItemParalel"></param>
        /// <param name="qty"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        private List<DataPointerStr> _SplitAxis(ExtenderDataSource data, KeyValuePair<CapacityPlanWorkItemCls, decimal> workItemParalel, decimal mnozstvi_zaplanovat, ref FunctionMenuItemRunArgs args)
        {                      
            List<DataPointerStr> result = new List<DataPointerStr>();
            List<KeyValuePair<int, WorkUnitCls>> workUnits = _GetWorkUnits(workItemParalel.Key); // pro vyrobni operaci dohledam pracovni jednotky (KPJ)
            if (workUnits.Count > 0) // Vyrobni operace ma pracovni jednotky (KPJ) ??
            {
                if (workItemParalel.Key.QtyRequired != mnozstvi_zaplanovat)  // zadané množství je jiné než na výrobní operaci parlelního pruchodu, rozdelim tedy materiálovou osu na dilci casti s definovaným množstvím
                {                                                          
                    workUnits.Sort(_CompareWorkUnitBegin); // setridim pracovni(kapacitní) jednotky podle času 
                    PlanningInteractiveSplitAxisSArgs splitArgs = new PlanningInteractiveSplitAxisSArgs();
                    splitArgs.SplitItemSource = workUnits[0].Value.DataPointer;           // zapisu ukazatel na prvni pracovni jednotku patřící pod výrobní operaci, kterou chci rozdělit                                                           
                    //splitArgs.SplitItemList.Add(new PlanningInteractiveSplitAxisSItemArgs(mnozstvi_zaplanovat, TimeRange.TimeDirection.ToFuture,true,1));
                    splitArgs.SplitItemList.Add(new PlanningInteractiveSplitAxisSItemArgs(mnozstvi_zaplanovat, TimeRange.TimeDirection.ToFuture, SplitAxisSQtyAdjustmentMode.None, 1));
                    decimal zbytek = Math.Max(0, workItemParalel.Key.QtyRequired - mnozstvi_zaplanovat);
                    if (zbytek > 0)
                        splitArgs.SplitItemList.Add(new PlanningInteractiveSplitAxisSItemArgs(zbytek, TimeRange.TimeDirection.ToFuture, true, 0));

                    data.PlanningProcess.PlanningData.InteractiveSplitAxisS(splitArgs);
                    splitArgs.ChangedRowsCopyTo(args);                   
                    //LJA
                    // do vyseledneho seznamu vyberu jen ty DataPointery na Elementy, ktere vznikly z polozky s hlavnim mnozstvim, pointery na elemnty, ktere vznikly z rozdiloveho mnozstvi neberu v uvahu
                    foreach (var si in splitArgs.SplitItemList.Where(item => item.Tag != null && (int)item.Tag > 0))                    
                        result.AddRange(si.ResultElementPointerList);                                   
                }
                else
                    result.Add(workUnits[0].Value.DataPointer); // mnozstvi na vzrobni operaci je stejne , vraci se pointer na prvni kapacitni jednotku
            }
            return result;
        }

        /// <summary>
        /// Rozdeleni jednoho paralelniho pruchodu na vic paralelnich průchodů  s definovanym mnozstvim
        /// Vraci seznma pointeru , kter vznikly rozdělním jednoho paralelního pruchodu na více dílčích průchodů
        /// </summary>
        /// <param name="data"></param>
        /// <param name="workItemParalel"> paralelni průchod, který se ma rozdělit</param>
        /// <param name="qty">mnozství na jednom paralelním průchodu</param>
        /// <param name="args"></param>
        /// <returns></returns>
        private List<DataPointerStr> _SplitTaskParalel(ExtenderDataSource data, KeyValuePair<CapacityPlanWorkItemCls, decimal> workItemParalel, decimal qty, ref FunctionMenuItemRunArgs args)
        {
            List<DataPointerStr> result = new List<DataPointerStr>();             
            //DAJ
            /*
             Pokud mam 100 ks rozdelit na 2 paralelni pruchody, pak se lis zvedne 50x!
             
             workItemParalel.Value => pocet parlelnich pruchodu jedne vyrobni operace.
             workItemParalel.Key.QtyRequired = > pocet kusu, ktere se maji touto vyrobni operaci vyrobit
             */
            List<KeyValuePair<int, WorkUnitCls>> workUnits = _GetWorkUnits(workItemParalel.Key); // pro vyrobni operaci vartim vsechny KPJ
            if (workUnits.Count > 0)
            {
                //decimal qtyTaskCurrent = ;         // celkove mnozstvi, ktere se ma na teto vyrobni operaci vyrobit
                workUnits.Sort(_CompareWorkUnitBegin);
                PlanningInteractiveSplitTaskCArgs splitTaskArgs = new PlanningInteractiveSplitTaskCArgs();
                splitTaskArgs.SplitItemSource = workUnits[0].Value.DataPointer; // podle prvni kapacitni jednotky definuju, ktery pruchod se bude delit



                // budu pridavat jednotive paralelni pruchody
                bool PridanaVyrovnanvaciPolozka = false;
                int i;
                for (i = 1; i <= workItemParalel.Value; i++)  // pro kazdý paralelní průchod pridam podilovou polozku 
                {
                    // decimal qtySplitSum = splitTaskArgs.SplitItemList.Sum(item => item.QtyRequired);   // Součet množství ze všech požadavků na dílcí paralelní průchody
                    if (workItemParalel.Key.QtyRequired >= (splitTaskArgs.QtyRequiredSum + qty)) // kontrola na pocet mnozstvi, nesmim zadat mnozstvi vyssi nez je celkove mnozstvi na vyrobni operaci
                    {
                        // kdyz pridam polozku, mnozstvi je mensi nez celkove
                        splitTaskArgs.SplitItemList.Add(new PlanningInteractiveSplitTaskCItemArgs(qty, new TimeVector(DateTime.MinValue, TimeRange.TimeDirection.ToFuture), i));
                    }
                    else
                    {
                        splitTaskArgs.SplitItemList.Add(new PlanningInteractiveSplitTaskCItemArgs(new TimeVector(DateTime.MinValue, TimeRange.TimeDirection.ToFuture), i));
                        PridanaVyrovnanvaciPolozka = true;
                        break;
                    }
                }

                // pokud je celkove mnoystvi vetsi a nebyla pridana vyrovnavaci polozka
                if (workItemParalel.Key.QtyRequired > splitTaskArgs.QtyRequiredSum && !PridanaVyrovnanvaciPolozka)
                {
                    splitTaskArgs.SplitItemList.Add(new PlanningInteractiveSplitTaskCItemArgs(new TimeVector(DateTime.MinValue, TimeRange.TimeDirection.ToFuture), i));
                }


                // DAJ
                // decimal qtySplitSum = splitTaskArgs.SplitItemList.Sum(i => i.QtyRequired);   // Součet množství ze všech požadavků na dílcí paralelní průchody

                data.PlanningProcess.PlanningData.InteractiveSplitTaskC(splitTaskArgs); // 
                foreach (PlanningInteractiveSplitTaskCItemArgs splitTaskArg in splitTaskArgs.SplitItemList)
                    result.AddRange(splitTaskArg.ResultElementPointerList);
                splitTaskArgs.ChangedRowsCopyTo(args);
            }
            return result;
        }

        /// <summary>
        /// Vrátí seznam dvojic: první nefixovaná vyrobni operace a pocet jeji vyskytu mezi polozkami konktretnich kombinaci
        /// pozn. Pro jednu konktretni kombinaci mohou existovat 2 a vice polozek, ktere maji shodnou PRVNI vyrobni operaci.
        /// </summary>
        /// <param name="combinItemsFirstWorkItem"></param>
        /// <returns></returns>
        private static Dictionary<CapacityPlanWorkItemCls, decimal> _GetWorkItemsParalel(Dictionary<PressFactCombinDataCls, CapacityPlanWorkItemCls> combinItemsFirstWorkItem)
        {
            Dictionary<CapacityPlanWorkItemCls, decimal> result = new Dictionary<CapacityPlanWorkItemCls, decimal>();
            IEnumerable<KeyValuePair<PressFactCombinDataCls, CapacityPlanWorkItemCls>> pom; // seznam parovych hodnot  Kombinace vylisku a prislusene vyrobni operace
            // vytvori seznam konkretnich vyrobni operaci a u kayde uvede pocet, kolikrat je operace obsazena v combinItemsFirstWorkItem 
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
            WorkUnitCls workUnit;          
            List<int> result = new List<int>();
            PlanningInteractiveMoveArgs  moveArgs = new PlanningInteractiveMoveArgs();
            moveArgs.CapacityLimitForActiveTask = LimitedCType.Unlimited;
            moveArgs.SetFixedTask = true; // operace se po presunuti budou fixovat
            moveArgs.PullAdjacentForActiveTask = true;
            moveArgs.PullAdjacentForActiveTree = true;
            CapacityUnitCls unit = _GetUnit(data, workplace);            
            
            foreach (DataPointerStr splitElement in splitElements)
            {               
                //workUnit = data.PlanningProcess.AxisHeap.FindIWorkItem(splitElement.Element);
                workUnit = data.PlanningProcess.PlanningData.FindWorkUnit(splitElement);
                if (!result.Contains(workUnit.TaskID))
                    result.Add(workUnit.TaskID); 
                // vartim novy casovz interval, kam se mam posunout
                TimeRange timeRange = workUnit.WorkTime.GetMovedToBegin(time);                              
                moveArgs.AddActiveItem(new PlanningInteractiveMoveActiveItem(workUnit, timeRange, unit.PlanUnitC, workplace));
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

        /// <summary>
        /// Setridi pracovní (kapacitní) jednotky podle času, v němž pracovní jedntka zahajuje svou práci ( setřídím podle času od nebližšího po nejvzdálenější)
        /// </summary>
        /// <param name="workUnit1"></param>
        /// <param name="workUnit2"></param>
        /// <returns></returns>
        private static int _CompareWorkUnitBegin(KeyValuePair<int, WorkUnitCls> workUnit1, KeyValuePair<int, WorkUnitCls> workUnit2)
        {
            int result;
            result = workUnit1.Value.WorkTime.Begin.CompareTo(workUnit2.Value.WorkTime.Begin);
            return result;
        }

        private void _Refresh(ExtenderDataSource data, List<DataPointerStr> splitElements, FunctionMenuItemRunArgs args)
        {
                                             
            // pridam identifikatory radku, ktere se zmenily. Kdyz se na tyto radky klikne, dojde k znovunacteni dat.
            GID gid;

            // aktualizuji data zvoleno radku - hlavickovy zaznam (0x4002) a vsechny naleyejici kombinace vylisku
            foreach (PressFactCombinDataCls combin in data.CombinData)
            {
               
                if (!args.ResultEditChangedRows.Contains(combin.ParentGID))
                    args.ResultEditChangedRows.Add(combin.ParentGID);
                args.ResultEditChangedRows.Add(combin.GID);
               
                ////gid = new GID(0x4002, combin.CisloSubjektu);
                ////if (!args.ResultEditChangedRows.Contains(gid))
                ////    args.ResultEditChangedRows.Add(gid);
                ////args.ResultEditChangedRows.Add(new GID(22290, combin.CisloObjektu));

            }
            args.ResultEditChangedRows.Add(new GID(0x4001, 1)); // zmena na radku pro vsechny lisy 
            
            //budu aktualizovat elemnty vsech konkretnich lisu
            foreach (int lisovaUnit in data.LisovnaUnits)
                args.ResultEditChangedRows.Add(new GID(PlanUnitCCls.ClassNr, lisovaUnit));
            
        }
        /// <summary>
        /// Pro vyrobni operaci zjistim souhrn pracovnij jednotek
        /// </summary>
        /// <param name="workItem"></param>
        /// <returns></returns>
        private List<KeyValuePair<int, WorkUnitCls>> _GetWorkUnits(CapacityPlanWorkItemCls workItem)
        {            
            List<KeyValuePair<int, WorkUnitCls>> result = new List<KeyValuePair<int, WorkUnitCls>>();
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

#define LOADONDEMAND
//#define TEST

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Drawing;
using System.Linq;
using Noris.Schedule.Planning.DataFace;
using Noris.Schedule.Planning.ProcessData;
using Noris.Schedule.Support;
using Noris.Schedule.Support.Core;
using Noris.Schedule.Support.Data;
using Noris.Schedule.Support.Services;
using Noris.Schedule.Support.Sql;



namespace Noris.Schedule.Extender
{
    #region PressFactCombinDataCls

    /// <summary>
    /// Polozka zaznamu tridy Konkrétní kombinace výlisků
    /// </summary>
    public class PressFactCombinDataCls
    {
        /// <summary>
        /// Identifikátor zaznamu
        /// </summary>
        public GID GID { get { return _CombinGID; } set { _CombinGID = value; } }
        private GID _CombinGID;

        public GID ParentGID { get { return _ParentGID; } set { _ParentGID = value; } }
        private GID _ParentGID;

        public int CisloSubjektu { get { return _CisloSubjektu; } set { _CisloSubjektu = value; } }
        private int _CisloSubjektu;
        /// <summary>
        /// Reference hlavicky konkretni kombinace vylisku
        /// </summary>
        public string Reference { get { return _Reference; } set { _Reference = value; } }
        private string _Reference;

        public string Nazev { get { return _Nazev; } set { _Nazev = value; } }
        private string _Nazev;
        /// <summary>
        /// Dilec na konkretni kombinaci
        /// SV 22876 (lcs.press_fact_combin_header.constr_element)
        /// </summary>
        public int ConstrElement { get { return _ConstrElement; } set { _ConstrElement = value; } }
        private int _ConstrElement;
        /// <summary>
        /// reference dilce (hlavicka)
        /// </summary>
        public string CERefer { get { return _CERefer; } set { _CERefer = value; } }
        private string _CERefer;
        /// <summary>
        /// nazev dilce (hlavicka)
        /// </summary>
        public string CENazev { get { return _CENazev; } set { _CENazev = value; } }
        private string _CENazev;
        /// <summary>
        /// identifikace polozky
        /// </summary>
        public int CisloObjektu { get { return _CisloObjektu; } set { _CisloObjektu = value; } }
        private int _CisloObjektu;
        /// <summary>
        /// Dilec na polozce konkretni kombinace 
        /// SV 22877 (lcs.press_fact_combin_item.constr_element)
        /// </summary>
        public int ConstrElementItem { get { return _ConstrElementItem; } set { _ConstrElementItem = value; } }
        private int _ConstrElementItem;
        /// <summary>
        /// Reference dilce na polozce konkretni kombinace
        /// </summary>
        public string CEItemRefer { get { return _CEItemRefer; } set { _CEItemRefer = value; } }
        private string _CEItemRefer;
        /// <summary>
        /// Nazev dilce na polozce konkretni kombinace
        /// </summary>
        public string CEItemNazev { get { return _CEItemNazev; } set { _CEItemNazev = value; } }
        private string _CEItemNazev;
        /// <summary>
        /// Pracoviště z DV 23546 Základní pracoviště
        /// </summary>
        public Dictionary<int, string> BaseWorkplace { get { return _BaseWorkplace; } set { _BaseWorkplace = value; } }
        public Dictionary<int, string> _BaseWorkplace;
        /// <summary>
        /// Pracoviště z DV 22919 Alternativní pracoviště
        /// </summary>
        public Dictionary<int, string> AlternativeWorkplaces { get { return _AlternativeWorkplaces; } set { _AlternativeWorkplaces = value; } }
        public Dictionary<int, string> _AlternativeWorkplaces;

        public PressFactCombinDataCls()
        {
            BaseWorkplace = new Dictionary<int, string>();
            AlternativeWorkplaces = new Dictionary<int, string>();
            AlternativeWorkplaces.Add(0, string.Empty);
        }
       
        public static int CompareByReference(PressFactCombinDataCls a, PressFactCombinDataCls b)
        {
            string comp1, comp2;

            comp1 = (a.Reference == null) ? String.Empty : a.Reference;
            comp2 = (b.Reference == null) ? String.Empty : b.Reference;
            return comp1.CompareTo(comp2);
        }
        /// <summary>
        /// Vybere vsechny polozky konkretni pro jednu konkretni kombinaci vylisku
        /// </summary>
        /// <param name="CombinData"></param>
        /// <param name="recordNumber"></param>
        /// <returns></returns>
        public static List<PressFactCombinDataCls> GetPressFactCombin(List<PressFactCombinDataCls> CombinData, int recordNumber)
        {

            // Z polozek konkretnich kombinaci vyberu vsechny s urcitym cislem_subjektu
            return CombinData.Where(item => item.CisloSubjektu == recordNumber).ToList<PressFactCombinDataCls>();
            /*
            List<PressFactCombinDataCls> result;
            result = CombinData.FindAll(
                delegate(PressFactCombinDataCls combin)
                {
                    return (combin.CisloSubjektu == recordNumber);
                });
            return result;*/
        }
    }
    #endregion

    public struct ConnParamStruct
    {
        public string ActiveUrl, SessionToken;
        public int ClassNumber;
        public List<int> RecordNumbers;
    }

    #region GraphDeclaration
    /// <summary>
	/// Deklarace grafu
	/// </summary>
	public class GraphDeclaration : Noris.Schedule.Support.Services.IGraphDeclarator
	{
		#region IGraphDeclarator Members
		/// <summary>
		/// Tady deklaruji grafy, které chci zobrazit
		/// </summary>
		/// <param name="args"></param>
		void IGraphDeclarator.GraphDeclare(GraphDeclareArgs args)
		{
            IDataSource dSource;
            Dictionary<int, string> lisovny;    //Všechna pracoviště s uda Zobrazit v PT = Ano 
            object tag = GRAPH_TAG;

            // Priorita: menší číslo = vyšší pozice. Grafy plánovací tabule mají prioritu 100. 
			//   Hodnota 200 bude tedy pod plánovací tabulí, 50 bude před ní.
			args.Priority = 200F;
			// Tady získám referenci na datový zdroj, který je vytovřený pro Plánovací proces:
			// Pokud se nenajde datový zdroj pro plánovací proces, pak se tato metoda (IGraphDeclarator.GraphDeclare) přeruší
			// a spustí se znovu o něco později. až bude datový zdroj pro plánovací proces existovat:
			IDataSource planningDs = args.GetExternalDataSource(typeof(MfrPlanningConnectorCls));
			// Tady si vytvořím svůj datový zdroj, který do sebe zapouzdří plánovací proces, ale on je zdrojem pro mé grafy:
			MfrPlanningConnectorCls planningProcess = planningDs as MfrPlanningConnectorCls;

            lisovny = _GetLisovny();   // 2 zaznamy -  VF lisy nadrazene  a Nosič - nadřazené pracoviště
            foreach (KeyValuePair<int, string> lisovna in lisovny)
            {
                dSource = new ExtenderDataSource(planningProcess, lisovna);
                GraphDeclarationCls topGraph, bottomGraph;
                // graf kapacitnich jednotek
                topGraph = new GraphDeclarationCls(RowGraphMode.TaskCapacityLink, PlanUnitCCls.ClassNr, dSource, tag, lisovna.Value, GraphPositionType.TopPart);
                topGraph.TargetGraphActivityOnClick = TargetGraphCrossActivityMode.OnThisPage;
                topGraph.CurrentGraphActivityOnClick = CurrentGraphCrossActivityMode.InactiveWhenInvisible | CurrentGraphCrossActivityMode.FindByDataSource | CurrentGraphCrossActivityMode.SelectElements | CurrentGraphCrossActivityMode.ActivateFirstEqualRow | CurrentGraphCrossActivityMode.ShowRelationNet | CurrentGraphCrossActivityMode.StopProcess;                                
                args.GraphDeclarationList.Add(topGraph);
                // graf planovanych Kombinaci
                bottomGraph  = new GraphDeclarationCls(RowGraphMode.TaskCapacityLink, 0x4002, dSource, tag, "Kombinace", GraphPositionType.BottomPart);
                bottomGraph.TargetGraphActivityOnClick= TargetGraphCrossActivityMode.OnThisPage;
                bottomGraph.CurrentGraphActivityOnClick = CurrentGraphCrossActivityMode.InactiveWhenInvisible | CurrentGraphCrossActivityMode.FindByDataSource | CurrentGraphCrossActivityMode.SelectElements | CurrentGraphCrossActivityMode.ActivateFirstEqualRow | CurrentGraphCrossActivityMode.SelectParentTopRow | CurrentGraphCrossActivityMode.OpenActiveTreeNodes | CurrentGraphCrossActivityMode.StopProcess;
                args.GraphDeclarationList.Add(bottomGraph);
                
            }
        }
		#endregion
		/// <summary>
		/// Tag do grafů, které se deklarují zde.
		/// Tento TAG přechází do dalších Services (například IGraphElementPainter) a slouží k identifikace "našeho" grafu oproti cizím grafům.
		/// </summary>
		public const string GRAPH_TAG = "GatemaGraf";


        private Dictionary<int, string> _GetLisovny()
        {
            Dictionary<int, string> result;
            string sql;
            DataTable dt;

            result = new Dictionary<int, string>();
            try
            {
                result = new Dictionary<int, string>();
                sql = "select w.cislo_subjektu, w.nazev_subjektu"
                    + " from lcs.uda_workplace uda"
                    + " join lcs.workplace w on uda.cislo_subjektu = w.cislo_subjektu"
                    + " where uda.zobrazit_v_pt = 'A'";
                dt = Db_Layer.GetDataTable(sql);
                foreach (DataRow row in dt.Rows)
                    result.Add(ExtenderDataSource.Get<int>(row, "cislo_subjektu"), ExtenderDataSource.Get<string>(row, "nazev_subjektu"));
            }
            catch
            {
            }

            return result;
        }
	}
	#endregion

    #region ExtenderDataSource
	/// <summary>
	/// Datový zdroj, který využívá služeb plánovacího procesu,
    /// Datovy zdroj je spojen s konktertnim grafem planovaci tabule
	/// </summary>
    public class ExtenderDataSource : IDataSource, IClassTreeExtender, IEvaluationDataSource
	{
        /// <summary>
        /// ExtGat
        /// </summary>
        private const string ToolTipSuffix = "ExtGat";
        public static ConnParamStruct ConnParam;
        /// <summary>
        /// Seznam všech konkrétních kombinací po položkách.
        /// </summary>
        public List<PressFactCombinDataCls> CombinData;
        ///// <summary>
        ///// Seznam všech spojovacích záznamů operace.
        
        /// <summary>
        /// Všechny KPJ se zdrojem pracoviště Lisovna 
        /// </summary>
        public List<int> LisovnaUnits;

        public KeyValuePair<int, string> Lisovna {get;private set;}
        public MfrPlanningConnectorCls PlanningProcess { get; private set; }

        public ExtenderDataSource()
        {            
        }

        public ExtenderDataSource(MfrPlanningConnectorCls planningProcess, KeyValuePair<int, string> lisovna)
        {
            PlanningProcess = planningProcess;
            Lisovna = lisovna;
        }

        #region IDataSource Members
		/// <summary>
		///     Aktuální informace, zda datový zdroj může přijmout asynchronní požadavek.
		///      Má vrátit true tehdy, pokud je na to naprogramován, a současně v delegátu
		///     this.RequestCompleted je odkaz na metodu, kterou je možno volat po dokončení
		///     requestu.
		/// </summary>
		bool IDataSource.AcceptRequestAsync { get { return false; } }
		
        /// <summary>
		///    Vrací počet čekajících asynchronních operací, typicky pro zobrazení ve stavovém
		///     řádku.  Pokud datový zdroj nepodporuje asynchronní operace (deklaruje CanBeAsynchronous
		///     = false), nechť vrátí 0.  Pokud datový zdroj podporuje asynchronní operace,
		///     může pro jejich řízení využít třídu Aps.Core.WorkQueueCls, kde je property
		///     AsynchronousOperationCount řádně implementována.  Pak stačí propojit property
		///     IDataSource.AsynchronousOperationCount na WorkQueueCls.AsynchronousOperationCount
		/// </summary>
        int IDataSource.AsynchronousOperationCount { get { return 0; } }    /* není asynchronní */
		
        /// <summary>
		///    Vlastnosti datového zdroje. Jde o statické informace.
		/// </summary>
		DataSourceProperties IDataSource.Properties
		{
			get
			{
				DataSourceProperties properties = new DataSourceProperties();
                properties.IsAsynchronous = true;
				properties.MakeRelationMap = true;
				return properties;
			}
		}

		/// <summary>
		///    Delegát na metodu, která bude spuštěna vždy poté, kdy je dokončena asynchronní
		///     práce na jednom každém požadavku.  Metoda dostává jako parametr ten požadavek,
		///     který byl předán k vyřízení.  Požadavek má vyplněné Result údaje.  Tento
		///     delegát se nevolá pro synchronní požadavky.  Delegát nemusí být vyplněn u
		///     datových zdrojů, které deklarují CanBeAsynchronous = false.
		/// </summary>
		Action<DataSourceRequest> IDataSource.RequestCompleted
		{
			get { return this._RequestCompleted; }
			set { this._RequestCompleted = value; }
		}
		private Action<DataSourceRequest> _RequestCompleted;

		/// <summary>
		///    Řízení asynchronní operace datového zdroje, umožní pozastavit / rozběhnout
		///     asynchronní operace.  false = default = asynchronní operace normálně probíhají
		///     / true = asynchronní operace jsou pozastaveny.  Pokud datový zdroj nepodporuje
		///     asynchronní operace (deklaruje CanBeAsynchronous = false), nechť implementuje
		///     standardní property bez rozšířené funkcionality.  Pokud datový zdroj podporuje
		///     asynchronní operace, může pro jejich řízení využít třídu Aps.Core.WorkQueueCls,
		///     kde je property WaitAsyncQueue řádně implementována.  Pak stačí propojit
		///     property IDataSource.WaitAsyncQueue na WorkQueueCls.WaitAsyncQueue
		/// </summary>
        /// 
		bool IDataSource.WaitAsyncQueue
		{
			get { return this._WaitAsyncQueue; }
			set { this._WaitAsyncQueue = value; }
		}

		private bool _WaitAsyncQueue;

		/// <summary>
		///    Event, který datový zdroj zavolá po každé změně počtu asynchronních požadavků.
		///      Pokud datový zdroj nepodporuje asynchronní operace (deklaruje CanBeAsynchronous
		///     = false), pak tento event sice musí obsahovat, ale nebude jej nikdy volat.
		///      Pokud datový zdroj pro řízení asynchronních operací využívá třídu Aps.Core.WorkQueueCls,
		///     pak bude volat event AsyncRequestCountChanged přímo z handleru eventu WorkQueueCls.WorkQueueCountChanged,
		///     včetně předání parametru e.
		/// </summary>
		event WorkQueueCountChangedDelegate IDataSource.AsyncRequestCountChanged
		{
			add { this._AsyncRequestCountChanged += value; }
			remove { this._AsyncRequestCountChanged -= value; }
		}
		private event WorkQueueCountChangedDelegate _AsyncRequestCountChanged;

		/// <summary>
		///    Vrací počet čekajících asynchronních operací, typicky pro zobrazení ve stavovém
		///     řádku.  Pokud datový zdroj nepodporuje asynchronní operace (deklaruje CanBeAsynchronous
		///     = false), nechť vrátí 0.  Pokud datový zdroj podporuje asynchronní operace,
		///     může pro jejich řízení využít třídu Aps.Core.WorkQueueCls, kde je property
		///     AsynchronousOperationCount řádně implementována.  Pak stačí propojit property
		///     IDataSource.AsynchronousOperationCount na WorkQueueCls.AsynchronousOperationCount
		/// </summary>
		/// <param name="activeGraphId"></param>
		void IDataSource.ActivateAsyncRequestForGraphId(int activeGraphId)
		{ /* Ukázkový datový zdroj není připravený na asynchronní operace (operace na pozadí). Je dostatečně rychlý. */ }

		/// <summary>
		///    Vrátí barvu pro kreslení určitého elementu
		/// </summary>
		/// <param name="graphType">Typ grafu</param>
		/// <param name="visualType">Vizuální typ elementu</param>
		/// <returns>Barva tohoto elementu</returns>
        Color IDataSource.GetColorForElement(RowGraphMode graphType, GraphElementVisualType visualType)
        {
            return Color.Black;
        }

		/// <summary>
		///     Metoda, která má za úkol vrátit soupis GID vedlejších prvků k danému prvku.
		///      Tato metoda se použije pouze v procesu editace, kdy editovaný blok dat (sada
		///     operací, atd) neobsahuje všechny prvky, na které se odkazuje.  Typicky to
		///     nastane v situaci, kdy prvek 30 se odkazuje na následující prvek 40, ale
		///     ten neexistuje.  Tato metoda pak musí vrátit GID prvku, který má navazovat
		///     na prvek 40 => tj. typicky půjde o prvek 50.  Pokud elementy, vrácené metodou
		///     RunRequestXxx(DataSourceRequestType.ElementsRead) vrací kompaktní skupiny
		///     dat, kde nejsou díry, pak zdejší metoda GetNearbyGIDs() může vždy vracet
		///     null.
		/// </summary>
		/// <param name="gID">GID výchozího prvku</param>
		/// <param name="orientation">Směr, v němž hledáme další prvky</param>
		/// <returns>Soupis GIDů, které v daném směru navazují. Může být null.</returns>
		IEnumerable<GID> IDataSource.GetNearbyGIDs(GID gID, ElementRelationOrientation orientation)
		{ return null; }

		/// <summary>
		///    Zařadí požadavek do fronty práce, nečeká na její dokončení, ihned vrací řízení.
		///     Po dokončení jsou výsledky odeslány do delegáta RequestCompleted.  Výsledky
		///     procesu jsou do něj předány v parametru request.  Pokud není vyplněn delegát
		///     RequestCompleted, je vyvolána chyba.  Metoda nemusí být funkčně implementována
		///     u datových zdrojů, které deklarují CanBeAsynchronous = false.
		/// </summary>
		/// <param name="request">Požadovaná operace</param>
		void IDataSource.RunRequestAsync(DataSourceRequest request)
		{
			this.RunRequest(request);
		}

		/// <summary>
		///    Zařadí požadavek do fronty práce, hned na její začátek, a počká na dokončení.
		///     Výsledky procesu jsou uloženy v parametru request.  Po dokončení není volán
		///     delegát RequestCompleted.
		/// </summary>
		/// <param name="request">Požadovaná operace</param>
		void IDataSource.RunRequestSync(DataSourceRequest request)
		{
            
			this.RunRequest(request);
		}
		#endregion

        public void RunRequest(DataSourceRequest request)
		{
            // touto metedou , ktera je soucasti interface IDatSource, tabule dava planovaci tabule datovemu zdroji pozadavek na nova data
            switch (request.RequestType)
            {
                case DataSourceRequestType.QueryAboutRequest:
                    _QueryAboutRequest(request);
                    break;
                case DataSourceRequestType.TopLevelRead:
                case DataSourceRequestType.SubRowsRead:
                    _ReadRows(request);
                    break;
                case DataSourceRequestType.ElementsRead:
#if (LOADONDEMAND)
                    DataSourceRequestReadElements requestElements = DataSourceRequest.TryGetTypedRequest<DataSourceRequestReadElements>(request);
                    requestElements.ResultElements.AddRange(_ReadElements(requestElements.RequestRowGId, TimeRange.Empty));         // DAJ : požádám o načtení VŠECH elementů, bez ohledu na requestElements
                    requestElements.ResultLoadAllElements = true;   // DAJ : víme, že jsme načetli VŠECHNY elementy:
#else
                    DataSourceRequestReadElements requestElements = DataSourceRequest.TryGetTypedRequest<DataSourceRequestReadElements>(request);
                    requestElements.ResultElements.AddRange(_ReadElements(requestElements.RequestRowGId, requestElements.RequestedTimeRange));
#endif
                    break;
                case DataSourceRequestType.FindInterGraphTargetData:
                    _FindInterGraphTargetData(request);
                    break;
                case DataSourceRequestType.CreateDataRelationNet:
                    _CreateRelations(request);
                    break;
                case DataSourceRequestType.RunDataFunction:
                    _RunDataFunction(request);
                    break;
            }
           
        }

#region QueryAboutRequest
        private void _QueryAboutRequest(DataSourceRequest requestInput)
        {
            DataSourceRequestQuery request;

            request = DataSourceRequest.TryGetTypedRequest<DataSourceRequestQuery>(requestInput);
            switch (request.QueryRequestType)
            {
                case DataSourceRequestType.SubRowsRead:
                case DataSourceRequestType.TopLevelRead:
                    request.QueryResultRunTimeout = 40 * 60;
                    break;
                case DataSourceRequestType.ActivateDataVersion:
                    if (CombinData == null)
                    {
                        request.QueryResultPostRequestList = new List<IComparable>();
                        request.QueryResultPostRequestList.Add(DataFunctionType.DataPrepare);
                        request.QueryResultPostRequestList.Add(DataFunctionType.DataLoad);
                    }
                    break;
                case DataSourceRequestType.RunDataFunction:
                    DataFunctionType functionType = (DataFunctionType)request.QueryRequestData;
                    switch (functionType)
                    {
                        case DataFunctionType.DataPrepare:
                            request.QueryResultProcessName = "Načtení výchozích podkladů pro plánování";
                            request.QueryResultRelativeTime = 0.50D;
                            request.QueryResultSkipThisProcess = false;
                            request.QueryResultActivateProgress = true;
                            break;
                        case DataFunctionType.DataLoad:
                            request.QueryResultProcessName = "Načtení dat plánu z databáze";
                            request.QueryResultRelativeTime = 1.00D;
                            request.QueryResultSkipThisProcess = false;
                            request.QueryResultActivateProgress = true;
                            break;
                        case DataFunctionType.DataSave:
                            request.QueryResultProcessName = "Uložení plánu do databáze";
                            request.QueryResultRelativeTime = 1.50D;
                            request.QueryResultSkipThisProcess = false;
                            break;
                    }
                    break;
            }
        }
#endregion

#region RunFunction
        private void _RunDataFunction(DataSourceRequest requestInput)
        {
            DataSourceRequestDataFunction request;
            DataFunctionType functionType;

            request = DataSourceRequest.TryGetTypedRequest<DataSourceRequestDataFunction>(requestInput);
            functionType = (DataFunctionType)request.RunRequestData;
            switch (functionType)
            {
                case DataFunctionType.DataPrepare:
                    ConnParam = _PrepareConnParam();
                    _PrepareCombinData();
                    break;
                case DataFunctionType.DataLoad:
                case DataFunctionType.DataRefresh:
                     _LoadCombinData();
                    break;
            }
        }
#endregion

#region PrepareData
        private ConnParamStruct _PrepareConnParam()
        {
            ConnParamStruct result;

            result = new ConnParamStruct();
            result.ActiveUrl = Steward.ParamGet("W", true);
            if (!String.IsNullOrEmpty(result.ActiveUrl) && !result.ActiveUrl.EndsWith("/ServiceGate.asmx", StringComparison.OrdinalIgnoreCase))
            {
                if (!result.ActiveUrl.EndsWith("/", StringComparison.OrdinalIgnoreCase))
                    result.ActiveUrl += "/ServiceGate.asmx";
                else
                    result.ActiveUrl += "ServiceGate.asmx";
            }
            result.SessionToken = Steward.ParamGet("S", true);

            return result;
        }

        private void _PrepareCombinData()
        {
            CombinData = new List<PressFactCombinDataCls>();
        }
#endregion

#region LoadData
        private void _LoadCombinData()
        {
            string sql;
            DataTable dt;
            PressFactCombinDataCls combin;

            CombinData.Clear();
            sql = "select pfh.cislo_subjektu, pfh.reference_subjektu, pfh.nazev_subjektu,"
                + " vsb.cislo_vztaz_subjektu base_workplace, wb.reference_subjektu base_workplace_refer, wb.nazev_subjektu base_workplace_nazev,"
                + " vs.cislo_vztaz_subjektu alternative_workplace, w.reference_subjektu alternative_workplace_refer, w.nazev_subjektu alternative_workplace_nazev,"
                + " pfh.constr_element ce, subj_1.reference_subjektu ce_refer, subj_1.nazev_subjektu ce_nazev,"
                + " pfi.cislo_objektu,"
                + " pfi.constr_element ce_item, subj_2.reference_subjektu ce_item_refer, subj_2.nazev_subjektu ce_item_nazev"
                + " from lcs.press_fact_combin_header pfh"
                + " join lcs.press_fact_combin_item pfi on pfh.cislo_subjektu = pfi.cislo_subjektu"
                + " join lcs.subjekty subj_1 on pfh.constr_element = subj_1.cislo_subjektu"
                + " join lcs.subjekty subj_2 on pfi.constr_element = subj_2.cislo_subjektu"
#if (TEST)
                + " left outer join lcs.vztahysubjektu vsb on vsb.cislo_subjektu = pfh.cislo_subjektu and vsb.cislo_vztahu = 23546"
                + " left outer join lcs.workplace wb on wb.cislo_subjektu = vsb.cislo_vztaz_subjektu and wb.parent_workplace = " + Lisovna.Key.ToString()
#else
                + " join lcs.vztahysubjektu vsb on vsb.cislo_subjektu = pfh.cislo_subjektu and vsb.cislo_vztahu = 23546"
                + " join lcs.workplace wb on wb.cislo_subjektu = vsb.cislo_vztaz_subjektu and wb.parent_workplace = " + Lisovna.Key.ToString()
#endif
                + " left outer join lcs.vztahysubjektu vs on pfh.cislo_subjektu = vs.cislo_subjektu and vs.cislo_vztahu = 22919"
                + " left outer join lcs.workplace w on vs.cislo_vztaz_subjektu = w.cislo_subjektu"
                + " where pfh.valid_combin = 'A'"
                + " order by pfh.cislo_subjektu, pfi.cislo_objektu";
            dt = Db_Layer.GetDataTable(sql);

            combin = new PressFactCombinDataCls();
            foreach (DataRow row in dt.Rows)
            {
                if (Get<int>(row, "cislo_objektu") != combin.CisloObjektu)
                {
                    if (combin.CisloObjektu > 0)
                    {
                        CombinData.Add(combin);
                        combin = new PressFactCombinDataCls();
                    }
                    combin.CisloSubjektu = Get<int>(row, "cislo_subjektu");
                    combin.Reference = Get<string>(row, "reference_subjektu");
                    combin.Nazev = Get<string>(row, "nazev_subjektu");
                    combin.ConstrElement = Get<int>(row, "ce");
                    combin.CERefer = Get<string>(row, "ce_refer");
                    combin.CENazev = Get<string>(row, "ce_nazev");
                    combin.CisloObjektu = Get<int>(row, "cislo_objektu");
                    combin.ConstrElementItem = Get<int>(row, "ce_item");
                    combin.CEItemRefer = Get<string>(row, "ce_item_refer");
                    combin.CEItemNazev = Get<string>(row, "ce_item_nazev");
                    if (Get<int>(row, "base_workplace") > 0)
                        combin.BaseWorkplace.Add(Get<int>(row, "base_workplace"), Get<string>(row, "base_workplace_refer") + " - " + Get<string>(row, "base_workplace_nazev"));
                    if (Get<int>(row, "alternative_workplace") > 0)
                        combin.AlternativeWorkplaces.Add(Get<int>(row, "alternative_workplace"), Get<string>(row, "alternative_workplace_refer") + " - " + Get<string>(row, "alternative_workplace_nazev"));
                }
                else // tady se radky namnozi kvuli navazanemu pracovisti
                    combin.AlternativeWorkplaces.Add(Get<int>(row, "alternative_workplace"), Get<string>(row, "alternative_workplace_refer") + " - " + Get<string>(row, "alternative_workplace_nazev"));
            }
            if (combin.CisloObjektu > 0)
                CombinData.Add(combin);

            using (var scope = Steward.TraceScopeBegin("Combin", "Load.Combin", Noris.Schedule.Planning.ProcessData.Constants.TraceKeyWordAplScheduler))
            {
                scope.User = new string[] { "Combin.Count = " + CombinData.Count.ToString() };
            }
        }
#endregion

#region ReadRows
        bool start = true;
        private void _ReadRows(DataSourceRequest requestInput)
        {                        
            DataSourceRequestReadRows request = DataSourceRequest.TryGetTypedRequest<DataSourceRequestReadRows>(requestInput);
            switch (request.RequestRowGId.ClassNumber)
            {
                case PlanUnitCCls.ClassNr:  // kapacitni jednotky (horni graf)
                    _ReadRowsKPJ(request);
                    break;
                case 0x4002:                // spodni graf
                    _ReadRowsCombin(request); // jednotlive konkretni kombinace
                    break;
            }
        }
        /// <summary>
        /// Nacte zaznamy o kapacitnich jednotkach (jednotlive lisy) a vykresli je do grafu
        /// </summary>
        /// <param name="request"></param>
        private void _ReadRowsKPJ(DataSourceRequestReadRows request)
        {
            this.LisovnaUnits = PlanningProcess.CapacityData.FindLinksToCapacityUnitForSource(Lisovna.Key).Select(item => item.PlanUnitC).ToList<int>(); /* vztahy na vsechny KPJ se zdrojem pracoviste Lisovna*/     
            PlanningVisualDataRowCls planningRow;                                
            // První řádek - společný pro všechny lisy
            GID rowGID = new GID(0x4001, 1); //třída = 0x4001, běžně se v databázích nepoužívá
            
            planningRow = new PlanningVisualDataRowCls(rowGID, RowGraphMode.TaskCapacityLink, "LISY", Lisovna.Value, false, String.Empty);
            planningRow.ActionOnDoubleClick = RowActionType.ZoomTimeToAllElements;// nastavim vlastnost graficke vrstvy radku, abzc pri poklepani se cely radek zazoomoval na vsechny 
            request.ResultItems.Add(planningRow);

#if (LOADONDEMAND)
            // DAJ : pokud tyto dva řádky vynechám, budou se načítat elementy "OnDemand":
            /*
            planningRow.ElementList = _ReadElements(planningRow.GId, TimeRange.Empty);
            planningRow.AllElementsLoaded = true;  
            */
#else
            planningRow.ElementList = _ReadElements(planningRow.GId, TimeRange.Empty);
            planningRow.AllElementsLoaded = true;
#endif

            // vrati cisla kapacitch planovacich jednotek(konkretnich lisu), ktere jsou navazany na konkretni lisovnu 

            // ze vsech kapacitnich jednotek vyberu jen ty, ktere patri do Lisovny a vytvorim pro ne radky do grafu        
            foreach (var unit in PlanningProcess.DataCapacityUnit.Values.Where(item => LisovnaUnits.Contains(item.PlanUnitData.RecordNumber)))
            {
                planningRow = new PlanningVisualDataRowCls(unit.PlanUnitData.GId, request.RequestedGraphMode, unit.PlanUnitData.Reference, _GetName(unit.PlanUnitC), false, String.Empty);
                request.ResultItems.Add(planningRow);
                planningRow.ActionOnDoubleClick = RowActionType.ZoomTimeToAllElements;

#if (LOADONDEMAND)
                // DAJ : pokud tyto dva řádky vynechám, budou se načítat elementy "OnDemand":
                /*
                planningRow.ElementList = _ReadElements(planningRow.GId,TimeRange.Empty);
                planningRow.AllElementsLoaded = true;                         
                */
#else
            planningRow.ElementList = _ReadElements(planningRow.GId,TimeRange.Empty);
            planningRow.AllElementsLoaded = true;
#endif
            }
        }

        private string _GetName(int unit)
        {
            string result;
            List<PlanCSourceLinkCls> links;
            SubjectCls source;

            result = String.Empty;
            links = PlanningProcess.CapacityData.FindLinksToSourceForCapacityUnit(unit);
            foreach (PlanCSourceLinkCls link in links.Where(item => item.CSource != Lisovna.Key))
            {
                source = PlanningProcess.CapacityData.FindCapacitySource(link.CSource);
                result = source.Nazev;
            }
            return result;
        }
        /// <summary>
        /// Nacten vsech komktertnich kombinaci vylisku do grafu
        /// </summary>
        /// <param name="request"></param>
        private void _ReadRowsCombin(DataSourceRequestReadRows request)
        {
            if (CombinData != null)
            {               
                GID parentRowGID, rowGID;
                PlanningVisualDataRowCls planningRow;               
                parentRowGID = GID.Empty;
             
                // dosel pozadavek na radky (subrows) pro urcity zaznam konkretni kombinace vylisku (TopRow). RecodrNumber v requestu je >0
                if (request.RequestRowGId.RecordNumber > 0) 
                {
                    // vsechny kombinace vylisku se stejnym cislem subjektu jako je TopRow
                    IEnumerable<PressFactCombinDataCls> combins = CombinData.Where(c => c.CisloSubjektu == request.RequestRowGId.RecordNumber); // vyberu vsechny kombinace, ktere nalezi k jednou zaznau grafu
                    // pridam prazdne radky do grafu, jako subradky k TopRow
                    foreach (PressFactCombinDataCls combin in combins)
                    {
                        combin.ParentGID = request.RequestRowGId;
                        rowGID = new GID(22290, combin.CisloObjektu);
                        combin.GID = rowGID;
                        // Vytvorim objekt reprezentujici zaznam kombinaci jako grafiky radek v grafu planovaci tabule
                        planningRow = new PlanningVisualDataRowCls(rowGID, RowGraphMode.TaskCapacityLink, combin.CEItemRefer, combin.CEItemNazev, false, String.Empty);
                        // Novemu radku rikam, ze jeho nadrazeny radek je radek, ktery zada o data
                        planningRow.ParentGId = request.RequestRowGId; 
                        planningRow.ActionOnDoubleClick = RowActionType.ZoomTimeToAllElements;
                        request.ResultItems.Add(planningRow);
#if (LOADONDEMAND)
                        // DAJ : pokud tyto dva řádky vynechám, budou se načítat elementy "OnDemand":
                        /*
                        planningRow.ElementList = _ReadElements(planningRow.GId,TimeRange.Empty);
                        planningRow.AllElementsLoaded = true;
                        */
#else
                        planningRow.ElementList = _ReadElements(planningRow.GId,TimeRange.Empty);
                        planningRow.AllElementsLoaded = true;
#endif
                    }
                }
                else // nactu vsechny zaznamy do grafu
                {
                    int lastCisloSubjektu = 0;
                    CombinData.Sort(PressFactCombinDataCls.CompareByReference); // setridim vsechny polozky
                    foreach (PressFactCombinDataCls combin in CombinData)
                    {
#if (LOADONDEMAND)
                        try
                        {
                            if (combin.CisloSubjektu == 2929897 /*GV000040-0003*/)  // if (combin.CisloSubjektu == 2929893 /*GV000040-0002*/)
                            { }
                            if (combin.CisloSubjektu != lastCisloSubjektu) // nalezena nova kombinace
                            {
                                parentRowGID = new GID(0x4002, combin.CisloSubjektu);
                                planningRow = new PlanningVisualDataRowCls(parentRowGID, RowGraphMode.TaskCapacityLink, combin.Reference, combin.Nazev, true, String.Empty);
                                planningRow.ActionOnDoubleClick = RowActionType.ZoomTimeToAllElements;
                                lastCisloSubjektu = combin.CisloSubjektu;

                                // DAJ : pokud tyto dva řádky vynechám, budou se načítat elementy "OnDemand":
                                /*
                                planningRow.ElementList = _ReadElements(planningRow.GId, TimeRange.Empty);
                                planningRow.AllElementsLoaded = true;
                                 */
                                request.ResultItems.Add(planningRow);
                            }
                            combin.ParentGID = parentRowGID;
                            rowGID = new GID(22290, combin.CisloObjektu);
                            combin.GID = rowGID;
                            planningRow = new PlanningVisualDataRowCls(rowGID, RowGraphMode.TaskCapacityLink, combin.CEItemRefer, combin.CEItemNazev, false, String.Empty);
                            planningRow.ParentGId = parentRowGID;
                            planningRow.ActionOnDoubleClick = RowActionType.ZoomTimeToAllElements;
                            request.ResultItems.Add(planningRow);
                            // nactu vsechny elementy do grafickeho radku
                            // DAJ : pokud tyto dva řádky vynechám, budou se načítat elementy "OnDemand":
                            /*
                            planningRow.ElementList = _ReadElements(planningRow.GId, TimeRange.Empty);
                            planningRow.AllElementsLoaded = true;
                             */
                        }
                        catch (Exception exc)
                        {   // Lokální nezávislý záznam chyby:
                            try
                            {
                                string eol = Environment.NewLine;
                                string txt = "";
                                Exception ex = exc;
                                string head = "Exception ";
                                while (ex != null)
                                {
                                    Type typ = ex.GetType();
                                    txt += head + typ.Namespace + "." + typ.Name + ": " + ex.Message + eol + ex.StackTrace + eol + "===============================================================" + eol;
                                    ex = exc.InnerException;
                                    head = "Inner exception ";
                                }
                                string time = DateTime.Now.ToString("yyyyMMdd-HHmmss");
                                string file = @"C:\Users\Administrator\Desktop\LCS - DAJ\logy\SchedulerError-" + time + ".txt";
                                System.IO.File.WriteAllText(file, txt, System.Text.Encoding.UTF8);
                            }
                            catch { }
                            throw;
                        }
#else
                        if (combin.CisloSubjektu != lastCisloSubjektu) // nalezena nova kombinace
                        {
                            parentRowGID = new GID(0x4002, combin.CisloSubjektu);
                            planningRow = new PlanningVisualDataRowCls(parentRowGID, RowGraphMode.TaskCapacityLink, combin.Reference, combin.Nazev, true, String.Empty);
                            planningRow.ActionOnDoubleClick = RowActionType.ZoomTimeToAllElements;                           
                            lastCisloSubjektu = combin.CisloSubjektu;
                            planningRow.ElementList = _ReadElements(planningRow.GId,TimeRange.Empty);
                            planningRow.AllElementsLoaded = true;
                            request.ResultItems.Add(planningRow);
                        }
                        combin.ParentGID = parentRowGID;
                        rowGID = new GID(22290, combin.CisloObjektu);
                        combin.GID = rowGID;
                        planningRow = new PlanningVisualDataRowCls(rowGID, RowGraphMode.TaskCapacityLink, combin.CEItemRefer, combin.CEItemNazev, false, String.Empty);
                        planningRow.ParentGId = parentRowGID;
                        planningRow.ActionOnDoubleClick = RowActionType.ZoomTimeToAllElements;
                        request.ResultItems.Add(planningRow);
                        // nactu vsechny elementy do grafickeho radku
                        planningRow.ElementList = _ReadElements(planningRow.GId, TimeRange.Empty);
                        planningRow.AllElementsLoaded = true;
#endif
                    }
                }
            }



            //if (CombinData != null)
            //{                
            //    int lastCisloSubjektu;
            //    GID parentRowGID, rowGID;
            //    PlanningVisualDataRowCls planningRow;

            //    lastCisloSubjektu = 0;
            //    parentRowGID = GID.Empty;
            //    CombinData.Sort(PressFactCombinDataCls.CompareByReference);
            //    foreach (PressFactCombinDataCls combin in CombinData)
            //    {
            //        if (combin.CisloSubjektu != lastCisloSubjektu)
            //        {
            //            parentRowGID = new GID(0x4002, combin.CisloSubjektu);
            //            planningRow = new PlanningVisualDataRowCls(parentRowGID, RowGraphMode.TaskCapacityLink, combin.Reference, combin.Nazev, true, String.Empty);
            //            planningRow.ActionOnDoubleClick = RowActionType.ZoomTimeToAllElements;
            //            request.ResultItems.Add(planningRow);
            //            lastCisloSubjektu = combin.CisloSubjektu;
            //        }
            //        combin.ParentGID = parentRowGID;
            //        rowGID = new GID(22290, combin.CisloObjektu);
            //        combin.GID = rowGID;
            //        planningRow = new PlanningVisualDataRowCls(rowGID, RowGraphMode.TaskCapacityLink, combin.CEItemRefer, combin.CEItemNazev, false, String.Empty);
            //        planningRow.ParentGId = parentRowGID;
            //        planningRow.ActionOnDoubleClick = RowActionType.ZoomTimeToAllElements;
            //        request.ResultItems.Add(planningRow);
            //    }
            //}
        }
#endregion

#region ReadElements
        ///// <summary>
        ///// Nacte do grafu graficke elementy Kapacitnich jednotek,kombinaci...
        ///// </summary>
        ///// <param name="requestInput"></param>
        //private void _ReadElements(DataSourceRequest requestInput)
        //{
        //    DataSourceRequestReadElements request;
            
        //    request = DataSourceRequest.TryGetTypedRequest<DataSourceRequestReadElements>(requestInput);
        //    switch (request.RequestRowGId.ClassNumber)
        //    {
        //        case 0x4001:              // Řádek "společný za více KPJ"
        //            foreach (int planUnitC in LisovnaUnits)
        //                _ReadElementsWorkUnit(request, planUnitC, false, false);
        //            break;
        //        case PlanUnitCCls.ClassNr:                // Řádek za konkrétní KPJ
        //            _ReadElementsWorkUnit(request, request.RequestRowGId.RecordNumber, true, true);
        //            break;
        //        case 0x4002:               // TopRows kombinaci
        //        case 22290:                // SubRows kombinace
        //            foreach (int planUnitC in LisovnaUnits)
        //                _ReadElementsWorkUnit(request, planUnitC, false, false);
        //            break;
        //    }
        //}


        private List<IDataElement> _ReadElements(GID rowGID,TimeRange interval)
        {
            List<IDataElement> elements = new List<IDataElement>();
            switch (rowGID.ClassNumber)
            {
                case 0x4001:              // Řádek "společný za více KPJ"
                    foreach (int planUnitC in LisovnaUnits)
                        elements.AddRange(_ReadElementsWorkUnit(rowGID, planUnitC, false, false, interval));
                    break;
                case PlanUnitCCls.ClassNr:                // Řádek za konkrétní KPJ
                    elements.AddRange(_ReadElementsWorkUnit(rowGID, rowGID.RecordNumber, true, true, interval));
                    break;
                case 0x4002:               // Parent Row kombinaci / redek ze vsechny polozky kombinace
                case 22290:                // SubRows kombinace
                    foreach (int planUnitC in LisovnaUnits)
                        elements.AddRange(_ReadElementsWorkUnit(rowGID, planUnitC, false, false, interval)); // budu zobrazovat pouze NEFIXOVANE stavy kapacit
                    break;
            }
            return elements;
        }


        private List<IDataElement> _ReadElementsWorkUnit(GID Row, int planUnitC, bool hasFixedTask, bool hasCreateLevel, TimeRange interval)
        {
            List<IDataElement> Elements = new List<IDataElement>();
            CapacityUnitCls unit;                       //KPJ
            List<CapacityLevelItemCls> levelList;       //stavy kapacit v danem obdobi pro danou KPJ
            PlanningVisualDataElementCls element;
            WorkUnitCls workUnit;
            PlanItemAxisS axis;

            PlanningProcess.DataCapacityUnit.TryGetValue(planUnitC, out unit);
            if (interval == TimeRange.Empty)
                levelList = unit.GetCurrentCapacityLevels();
            else
                levelList = unit.GetCapacityLevels(interval);
            foreach (CapacityLevelItemCls level in levelList)
            {
                if (hasCreateLevel)
                {
                    //jeden stav kapacit jedne KPJ
                    element = _GetElementLevel(Row, level);
                    Elements.Add(element);
                }

                //všechny kapacitní úkoly vsech pracovnich linek jednoho stavu kapacit jedne KPJ
                foreach (CapacityDeskCls desk in level.DeskArrayAll)        //pres vsechny pracovni linky jednoho stavu kapacit; DAJ: DeskArrayAll = všechny pracovní linky, tj. standardní (DeskArrayStd) + navýšená (DeskArrayInc) kapacita
                {
                    if (desk == null) continue;                             // DAJ: pole někdy může obsahovat NULL prvky
                    foreach (CapacityJobItemCls job in desk.JobList)        //pres vsechny ukoly jedne pracovni linky
                    {
                        workUnit = (WorkUnitCls)job.WorkUnit;
                        if (workUnit.IsFixedTask == hasFixedTask)
                        {
                            PressFactCombinDataCls c = null;

                            axis = PlanningProcess.AxisHeap.FindAxisSItem(job.AxisID);
                            switch (Row.ClassNumber)
                            {
                                case 0x4001:    //vsechny KPJ dohromady
                                    c = new PressFactCombinDataCls();
                                    break;
                                case PlanUnitCCls.ClassNr:    //jednotlive KPJ
                                    if (Row.RecordNumber == workUnit.CapacityUnit)
                                        c = new PressFactCombinDataCls();
                                    break;
                                case 0x4002:    //hlavicka kombinaci
                                    c = CombinData.Find(
                                        delegate(PressFactCombinDataCls combin)
                                        {
                                            return (combin.CisloSubjektu == Row.RecordNumber
                                                && combin.ConstrElementItem == axis.ConstrElement);
                                        }
                                    );
                                    break;
                                case 22290:     //polozky kombinaci
                                    c = CombinData.Find(
                                        delegate(PressFactCombinDataCls combin)
                                        {
                                            return (combin.CisloObjektu == Row.RecordNumber
                                                && combin.ConstrElementItem == axis.ConstrElement);
                                        }
                                    );
                                    break;
                            }
                            if (c != null)
                            {
                                element = GetElementsWorkUnit(Row, workUnit);
                                Elements.Add(element);
                            }
                        }
                    }
                }
            }
            return Elements;
        }


        //private void _ReadElementsWorkUnit(DataSourceRequestReadElements request, int planUnitC, bool hasFixedTask, bool hasCreateLevel)
        //{
        //    CapacityUnitCls unit;                       //KPJ
        //    List<CapacityLevelItemCls> levelList;       //stavy kapacit v danem obdobi pro danou KPJ
        //    PlanningVisualDataElementCls element;
        //    WorkUnitCls workUnit;
        //    MaterialPlanAxisItemCls axis;
            
        //    PlanningProcess.DataCapacityUnit.TryGetValue(planUnitC, out unit);
        //    levelList = unit.GetCapacityLevels(request.RequestedTimeRange);  //vsechny stavy kapacit jedne KPJ v danem obdobi
        //    foreach (CapacityLevelItemCls level in levelList)
        //    {
        //        if (hasCreateLevel)
        //        {
        //            //jeden stav kapacit jedne KPJ
        //            element = _GetElementLevel(request, level);                         
        //            request.ResultElements.Add(element);
        //        }

        //        //všechny kapacitní úkoly vsech pracovnich linek jednoho stavu kapacit jedne KPJ
        //        foreach (CapacityDeskCls desk in level.CapacityDesk)        //pres vsechny pracovni linky jednoho stavu kapacit
        //        {
        //            foreach (CapacityJobItemCls job in desk.JobList)        //pres vsechny ukoly jedne pracovni linky
        //            {
        //                workUnit = (WorkUnitCls)job.WorkUnit;
        //                if (workUnit.IsFixedTask == hasFixedTask)
        //                {
        //                    PressFactCombinDataCls c = null;

        //                    axis = PlanningProcess.AxisHeap.FindAxisSItem(job.AxisID);
        //                    switch (request.RequestRowGId.ClassNumber)
        //                    {
        //                        case 0x4001:    //vsechny KPJ dohromady
        //                            c = new PressFactCombinDataCls();
        //                            break;
        //                        case PlanUnitCCls.ClassNr:    //jednotlive KPJ
        //                            if (request.RequestRowGId.RecordNumber == workUnit.CapacityUnit)
        //                                c = new PressFactCombinDataCls();
        //                            break;
        //                        case 0x4002:    //hlavicka kombinaci
        //                            c = CombinData.Find(
        //                                delegate(PressFactCombinDataCls combin)
        //                                {
        //                                    return (combin.CisloSubjektu == request.RequestRowGId.RecordNumber
        //                                        && combin.ConstrElementItem == axis.ConstrElement);
        //                                }
        //                            );
        //                            break;
        //                        case 22290:     //polozky kombinaci
        //                            c = CombinData.Find(
        //                                delegate(PressFactCombinDataCls combin)
        //                                {
        //                                    return (combin.CisloObjektu == request.RequestRowGId.RecordNumber
        //                                        && combin.ConstrElementItem == axis.ConstrElement);
        //                                }
        //                            );
        //                            break;
        //                    }
        //                    if (c != null)
        //                    {
        //                        element = GetElementsWorkUnit(request.RequestRowGId, workUnit);
        //                        request.ResultElements.Add(element);
        //                    }
        //                }
        //            }
        //        }
        //    }
        //}

        private PlanningVisualDataElementCls _GetElementLevel(DataSourceRequestReadElements request, CapacityLevelItemCls level)
        {
            PlanningVisualDataElementCls result;
            GID elementGID, rowGID, workGID;
            TimeRange timeRange;

            elementGID = new GID(1365, level.LevelID);
            rowGID = request.RequestRowGId;
            workGID = GID.Empty;
            timeRange = level.Time;
            result = new PlanningVisualDataElementCls(elementGID, rowGID, 0, workGID, level.Time);
            result.SuffixKeyForToolTip = ToolTipSuffix;
            result.SetElementProperty(GraphElementLayerType.SubLayer, DataElementEditingMode.FixedLinkItemsOnSameThread, GraphElementVisualType.Type1);

            return result;
        }

        private PlanningVisualDataElementCls _GetElementLevel(GID rowGID, CapacityLevelItemCls level)
        {
            PlanningVisualDataElementCls result;
            GID elementGID, workGID;
            TimeRange timeRange;

            elementGID = new GID(1365, level.LevelID);
           // rowGID = request.RequestRowGId;
            workGID = GID.Empty;
            timeRange = level.Time;
            result = new PlanningVisualDataElementCls(elementGID, rowGID, 0, workGID, level.Time);
            result.SuffixKeyForToolTip = ToolTipSuffix;
            result.SetElementProperty(GraphElementLayerType.SubLayer, DataElementEditingMode.FixedLinkItemsOnSameThread, GraphElementVisualType.Type1);

            return result;
        }

        public static PlanningVisualDataElementCls GetElementsWorkUnit(GID rowGID, WorkUnitCls workUnit)
        {
            PlanningVisualDataElementCls result;
            GID elementGID, workGID;
            TimeRange timeRange;
            DataElementEditingMode editMode;

            elementGID = new GID(Planning.ProcessData.Constants.ClassNumberWork, workUnit.WorkID);
            workGID = new GID(Planning.ProcessData.Constants.ClassNumberTask, workUnit.TaskID);
            timeRange = workUnit.WorkTime;            
           
            
            result = new PlanningVisualDataElementCls(elementGID, rowGID, 1817, workGID, timeRange);            
            result.SuffixKeyForToolTip = ToolTipSuffix;
            editMode = DataElementEditingMode.Movable;
            result.UseRatio = (float)workUnit.UseRatio;
            result.LinkGroup = workUnit.TaskData.LinkGroup;
            result.SetElementProperty(GraphElementLayerType.ItemLayer, editMode, GraphElementVisualType.Type1);
            result.SetColor((rowGID.ClassNumber == 22290) ? Color.Orange : Color.Aqua);
            result.TextNote = "Axis :" + workUnit.AxisID + ", Task :" + workUnit.TaskID + ", Work :" + workUnit.WorkID + ", Požadováno :" + workUnit.QtyRequired;

            return result;
        }


       

#endregion
        
#region FindTarget
        private void _FindInterGraphTargetData(DataSourceRequest requestInput)
        {
            DataSourceRequestFindInterGraph request;
            
            request = DataSourceRequest.TryGetTypedRequest<DataSourceRequestFindInterGraph>(requestInput);
            if (request.SourceElement.Element.ClassNumber == Planning.ProcessData.Constants.ClassNumberWork)
            {
                int workID;
                WorkUnitCls homeWork;
                PlanItemTaskC task;
                PlanItemAxisS axis;
                DataPointerStr pointer;

                workID = request.SourceElement.Element.RecordNumber;
                homeWork = PlanningProcess.AxisHeap.FindIWorkItem(workID);
                task = PlanningProcess.AxisHeap.FindTaskCItem(homeWork.TaskID);
                axis = PlanningProcess.AxisHeap.FindAxisSItem(task.AxisID);
                request.ResultSelectElementList = new List<DataPointerStr>();
                switch (request.SourceElement.Row.ClassNumber)
                {
                    case 0x4001:
                        if (homeWork != null)
                        {
                            PressFactCombinDataCls c;
                            c = CombinData.Find(delegate(PressFactCombinDataCls combin)
                            {
                                return (combin.ConstrElementItem == axis.ConstrElement);
                            });
                            if (c != null)
                            {
                                foreach (WorkPassCls workPass in task.WorkPassList)
                                    foreach (WorkTimeCls workTime in workPass.WorkTimeList)
                                        foreach (WorkUnitCls workUnit in workTime.WorkUnitList)
                                        {
                                            pointer = new DataPointerStr(c.ParentGID, workUnit.DataPointer.Element);
                                            request.ResultSelectElementList.Add(pointer);
                                        }
                            }
                        }
                        break;
                    case 0x4002:
                    case 22290:
                        foreach (WorkPassCls workPass in task.WorkPassList)
                            foreach (WorkTimeCls workTime in workPass.WorkTimeList)
                                foreach (WorkUnitCls workUnit in workTime.WorkUnitList)
                                {
                                    pointer = new DataPointerStr(new GID(0x4001, 1), workUnit.DataPointer.Element);
                                    request.ResultSelectElementList.Add(pointer);
                                }
                        break;
                }
            }
        }
#endregion

#region CreateRelations
        private void _CreateRelations(DataSourceRequest requestInput)
        {
            DataSourceRequestReadRelations request;

            request = DataSourceRequest.TryGetTypedRequest<DataSourceRequestReadRelations>(requestInput);
            if (request.RequestElementPointer.Element.ClassNumber == Planning.ProcessData.Constants.ClassNumberWork)
            {
                int workID;
                WorkUnitCls workUnit;
                PlanItemTaskC task;
                WorkTimeCls lastTime;

                workID = request.RequestElementPointer.Element.RecordNumber;
                request.ResultRelations = new List<Support.Services.DataRelation>();
                workUnit = PlanningProcess.AxisHeap.FindIWorkItem(workID);
                task = PlanningProcess.AxisHeap.FindTaskCItem(workUnit.TaskID);
                foreach (WorkPassCls workPass in task.WorkPassList)
                {
                    lastTime = null;
                    foreach (WorkTimeCls workTime in workPass.WorkTimeList)
                    {
                        _CreateOneWorkTimeItem(workTime, ref request);
                        if (lastTime != null)
                           _CreateOneWorkTimeRelation(lastTime, workTime, ref request);
                        lastTime = workTime;
                    }
                }
            }
        }

        private void _CreateOneWorkTimeItem(WorkTimeCls workTime, ref DataSourceRequestReadRelations request)
        {
            DataPointerStr pointer, pointerLast;
            WorkUnitCls lastWorkUnit;
            GID rowGID;

            lastWorkUnit = null;
            foreach (WorkUnitCls workUnit in workTime.WorkUnitList)
            {
                rowGID = request.RequestElementPointer.Row;

                // 1. Informace o samotné jednotce WorkUnit
                pointer = new DataPointerStr(rowGID, workUnit.DataPointer.Element);
                request.ResultRelations.Add(new Support.Services.DataRelation(pointer));

                // 2. Informace o paralelním vztahu:
                if (lastWorkUnit != null)
                {
                    pointerLast = new DataPointerStr(rowGID, lastWorkUnit.DataPointer.Element);
                    request.ResultRelations.Add(new Support.Services.DataRelation(pointerLast, ElementRelationOrientation.Parallel,false, pointer));
                }
                lastWorkUnit = workUnit;
            }
        }

        private void _CreateOneWorkTimeRelation(WorkTimeCls lastWorkTime, WorkTimeCls workTime, ref DataSourceRequestReadRelations request)
        {
            if (lastWorkTime.WorkUnitList.Count > 0 && workTime.WorkUnitList.Count > 0)
            {
                bool reverse;
                ReadOnlyCollection<WorkUnitCls> leftWorkUnits, rightWorkUnits;
                WorkUnitCls rightWorkUnit;
                DataPointerStr pointerLeft, pointerRight;
                List<bool> isPairRight;
                int n;
                GID rowGID;

                reverse = (workTime.WorkUnitList.Count > lastWorkTime.WorkUnitList.Count);
                leftWorkUnits = null;
                rightWorkUnits = null;
                leftWorkUnits = (reverse ? workTime : lastWorkTime).WorkUnitList;
                rightWorkUnits = (reverse ? lastWorkTime : workTime).WorkUnitList;
                isPairRight = new List<bool>();
                foreach (WorkUnitCls workUnit in rightWorkUnits)
                    isPairRight.Add(false);

                foreach (WorkUnitCls leftWorkUnit in leftWorkUnits)
                {
                    rightWorkUnit = rightWorkUnits.First(workUnit =>
                                (workUnit.CapacitySource == leftWorkUnit.CapacitySource));
                    if (rightWorkUnit != null)
                    {
                        n = rightWorkUnits.IndexOf(rightWorkUnit);
                        if (!isPairRight[n])
                        {
                            isPairRight[n] = true;
                            rowGID = request.RequestElementPointer.Row;
                            pointerRight = new DataPointerStr(rowGID, rightWorkUnit.DataPointer.Element);
                            pointerLeft = new DataPointerStr(rowGID, leftWorkUnit.DataPointer.Element);
                            if (!reverse)
                                request.ResultRelations.Add(new Support.Services.DataRelation(pointerLeft, ElementRelationOrientation.Parallel, false, pointerRight));
                            else
                                request.ResultRelations.Add(new Support.Services.DataRelation(pointerRight, ElementRelationOrientation.Parallel, false, pointerLeft));
                        }
                    }
                }
            }
        }
#endregion

#region POMOCNÉ METODY A DATA
		public static T Get<T>(DataRow row, string column)
		{
			if (row.IsNull(column))
				return default (T);
			return (T)row[column];
		}
#endregion

#region IEvaluationDataSource Members

        public bool TryGetDataObject(EvaluationDataSourceGetObjectArgs args)
        {
            return ((IEvaluationDataSource)PlanningProcess).TryGetDataObject(args);
        }

        public bool TryGetDataRelated(EvaluationDataSourceGetRelatedObjectArgs args)
        {
            return ((IEvaluationDataSource)PlanningProcess).TryGetDataRelated(args);
        }

        public bool TryGetValue(EvaluationDataSourceGetValueArgs args)
        {
            return ((IEvaluationDataSource)PlanningProcess).TryGetValue(args);
        }

#endregion

#region IClassTreeExtender Members

        public void GetExtendedAttributes(ClassTreeExtenderGetAttributesArgs args)
        {
            ((IClassTreeExtender)PlanningProcess).GetExtendedAttributes(args);

        }

        public void GetExtendedRelations(ClassTreeExtenderGetRelationsArgs args)
        {
            ((IClassTreeExtender)PlanningProcess).GetExtendedRelations(args);
        }

#endregion
    }
#endregion

#region DEFAULT PAINTER - pro graf TaskCapacityLink a vrstvu ItemLayer
    /// <summary>
    /// Painter pro Item elementy grafu TaskCapacity
    /// </summary>
    internal class PainterTaskItemElement : IGraphElementPainter
    {
        /// <summary>
        /// Zde malíř elementu vyjadřuje svoji touhu kreslit konkrétní typ položky grafu (elementu).
        /// Na vstupu je uvedena specifikace elementu: typ grafu, číslo třídy elementu, typ elementu.
        /// Zdejší objekt vyhodnotí zadané údaje a rozhodne se, zda bude chtít daný typ elementu kreslit.
        /// Toto rozhodnutí platí pro všechny takto specifikované elementy.
        /// Pokud nechce malovat, vrátí 0.
        /// Pokud se sejde více tříd malířů, které by chtěly kreslit stejný element, pak vyhraje ta, která vrací vyšší číslo.
        /// Tato metoda se volá nejvýše jedenkrát za života jednoho grafu, vrácená hodnota platí pro všechny elementy tohoto grafu.
        /// </summary>
        /// <param name="elementKey">Specifikace elementu grafu</param>
        /// <returns>Hodnota vyjadřující moji touhu kreslit tento element</returns>
        float IGraphElementPainter.GetPriority(GraphElementPainterKey elementKey)
        {
            bool active = (
                elementKey.GraphMode == RowGraphMode.TaskCapacityLink &&
                elementKey.LayerType == GraphElementLayerType.ItemLayer &&
                (elementKey.GraphTag is string && ((string)elementKey.GraphTag) == GraphDeclaration.GRAPH_TAG));
            return (active ? 5.0F : 0);
        }
        /// <summary>
        /// Výkonná metoda kreslení elementu.
        /// Tady se painter může projevit.
        /// Anebo může nadefinovat sadu dat do argumentu, a jádro pak podle těchto dat vykreslí element.
        /// </summary>
        /// <param name="args"></param>
        void IGraphElementPainter.Paint(GraphElementPainterArgs args)
        {
            // Vlastní prostor reprezentace grafu:
            args.DetailArea = args.DefaultDetailAreaElement;         // Vypočítat prostor pro detail
            args.BrushArea = args.RowGraphArea;                      // Prostor štětce = podle prostoru řádku
            args.VisualRectangle = args.DefaultVisualRectangle;      // Vizuální prostor je defaultní

            // Pozadí:
            args.BackGroundRunSystem = true;
            args.BackGroundRectangle = args.DetailArea;
            args.BackGroundColor = args.DataElement.BackColor;       // Barva elementu, kterou jsme do něj sami vložili

            // Okraje pozadí:
            args.BackLineRunSystem = true;
            args.BackLineColor = Color.Black;
            args.BackLineWidth = 1;

            // Pokud je element v editoru (tzn. je propojen v síti vztahů), 
            // anebo je vybraný (Selected),
            //  pak bude mít výraznější okraje (dvojité) a část svého vnitřku věnuje právě těmto okrajům:
            if (args.ElementState == GraphElementState.InEditorFree ||
                args.ElementState == GraphElementState.SelectedFree)
            {
                Rectangle back = args.BackGroundRectangle;
                back.Y = back.Y + 1;
                back.Height = back.Height - 2;
                args.BackGroundRectangle = back;

                args.BackLineColor = (args.RowItem.GId.ClassNumber == 22290) ? Color.Brown : Color.DarkBlue; //barva ohraniceni ramecku
                args.BackLineWidth = 1;

                args.OuterLineRunSystem = true;
                args.OuterLineColor = (args.RowItem.GId.ClassNumber == 22290) ? Color.Brown : Color.DarkBlue;   //barva ramecku
                args.BackGroundColor = (args.RowItem.GId.ClassNumber == 22290) ? Color.Brown : Color.DarkBlue;  //barva elementu
                args.OuterLineWidth = 4;
            }

            // Popisek nekreslit v aktivním grafu:
            if (args.ElementState == GraphElementState.OnMouseFree ||
                args.ElementState == GraphElementState.SelectedFree ||
                args.ElementState == GraphElementState.InEditorFree ||
                args.ElementState == GraphElementState.ActiveFree)
            {
                args.CaptionRunSystem = false;
                args.CaptionColor = Color.Black;
            }
        }

        void IGraphElementPainter.PaintAfter(GraphElementPainterArgs args)
        { }
    }
#endregion



/*
    class aaa : IDataSource
    {
#region IDataSource Members

        public DataSourceProperties Properties
        {
            get { throw new NotImplementedException(); }
        }

        public bool AcceptRequestAsync
        {
            get { throw new NotImplementedException(); }
        }

        public void RunRequestSync(DataSourceRequest request)
        {
            throw new NotImplementedException();
        }

        public void RunRequestAsync(DataSourceRequest request)
        {
            throw new NotImplementedException();
        }

        public bool WaitAsyncQueue
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public int AsynchronousOperationCount
        {
            get { throw new NotImplementedException(); }
        }

        public void ActivateAsyncRequestForGraphId(int activeGraphId)
        {
            throw new NotImplementedException();
        }

        public Action<DataSourceRequest> RequestCompleted
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public event WorkQueueCountChangedDelegate AsyncRequestCountChanged;

        public Color GetColorForElement(RowGraphMode graphType, GraphElementVisualType visualType)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<GID> GetNearbyGIDs(GID gID, ElementRelationOrientation orientation)
        {
            throw new NotImplementedException();
        }

#endregion
    }
    */
}

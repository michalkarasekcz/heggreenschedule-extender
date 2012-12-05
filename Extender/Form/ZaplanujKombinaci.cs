using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Noris.Schedule.Planning.ProcessData; 

namespace Noris.Schedule.Extender
{
    public partial class ZaplanujKombinaci : Form
    {
        public bool OK;
        public ExtenderDataSource Data;
        public Dictionary<PressFactCombinDataCls, CapacityPlanWorkItemCls> CombinItemsFirstTask;
        public decimal Pocet_zalisu{get;private set;}
        public DateTime StartTime;
        public List<KeyValuePair<int, string>> WorkplaceList;
        public int Workplace;

        public ZaplanujKombinaci(ExtenderDataSource data, Dictionary<PressFactCombinDataCls, CapacityPlanWorkItemCls> combinItemsFirstWorkItem, decimal pocet_zalisu, List<KeyValuePair<int, string>> workplaceList)
        {
            InitializeComponent();
            AcceptButton = okBtn;
            CancelButton = stornoBtn;

            Data = data;
            CombinItemsFirstTask = combinItemsFirstWorkItem;
            Pocet_zalisu = pocet_zalisu;
            WorkplaceList = workplaceList;
        }

        private void _FillParams(object sender, EventArgs e)
        {
            qtyTbx.Text = Pocet_zalisu.ToString();
            workplaceCbx.DataSource = WorkplaceList;
        }

        private void _Validate(object sender, EventArgs e)
        {
            OK = true;
            Close();
        }

        private void _Novalidate(object sender, EventArgs e)
        {
            OK = false;
        }

        private void _ReturnParams(object sender, FormClosedEventArgs e)
        {
            Pocet_zalisu = Convert.ToDecimal(qtyTbx.Text);
            StartTime = startTimeDtp.Value;
            Workplace = (workplaceCbx.SelectedItem == null) ? 0 : ((KeyValuePair<int, string>)workplaceCbx.SelectedItem).Key;
        }

        private void _FillStartTime(object sender, EventArgs e)
        {
            startTimeDtp.Value = PlanCombin.GetStartTime(Data, CombinItemsFirstTask, WorkplaceList[workplaceCbx.SelectedIndex].Key);
        }
    }
}

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
        public Dictionary<PressFactCombinDataCls, PlanItemTaskC> CombinItemsFirstTask;
        public decimal Pocet_zalisu{get;private set;}
        public DateTime StartTime;
        public List<KeyValuePair<int, string>> BaseWorkplaceList;
        public List<KeyValuePair<int, string>> AlternativeWorkplaceList;
        public int Workplace;

        public ZaplanujKombinaci(ExtenderDataSource data, Dictionary<PressFactCombinDataCls, PlanItemTaskC> combinItemsFirstWorkItem, decimal pocet_zalisu, List<KeyValuePair<int, string>> baseWorkplaceList, List<KeyValuePair<int, string>> alternativeWorkplaceList)
        {
            InitializeComponent();
            AcceptButton = okBtn;
            CancelButton = stornoBtn;

            Data = data;
            CombinItemsFirstTask = combinItemsFirstWorkItem;
            Pocet_zalisu = pocet_zalisu;
            BaseWorkplaceList = baseWorkplaceList;
            AlternativeWorkplaceList = alternativeWorkplaceList;
        }

        private void _FillParams(object sender, EventArgs e)
        {
            qtyTbx.Text = Pocet_zalisu.ToString();
            
            baseWorkplaceCbx.DataSource = BaseWorkplaceList;
            baseWorkplaceCbx.DisplayMember = "Value";

            alternativeWorkplaceCbx.DataSource = AlternativeWorkplaceList;
            alternativeWorkplaceCbx.DisplayMember = "Value";
        }

        private void _Validate(object sender, EventArgs e)
        {
            OK = true;
            Close();
        }

        private void _NoValidate(object sender, EventArgs e)
        {
            OK = false;
        }

        private void _ReturnParams(object sender, FormClosedEventArgs e)
        {
            Pocet_zalisu = Convert.ToDecimal(qtyTbx.Text);
            StartTime = startTimeDtp.Value;
            //Pracoviště - Bere se Základní pracoviště, pokud není vyplněno Alternativní pracoviště
            Workplace = baseWorkplaceCbx.SelectedItem != null ? BaseWorkplaceList[baseWorkplaceCbx.SelectedIndex].Key : 0;
            if (alternativeWorkplaceCbx.SelectedItem != null && AlternativeWorkplaceList[alternativeWorkplaceCbx.SelectedIndex].Key > 0)
                Workplace = AlternativeWorkplaceList[alternativeWorkplaceCbx.SelectedIndex].Key;
        }

        private void _FillStartTime(object sender, EventArgs e)
        {
            int workplace = baseWorkplaceCbx.SelectedItem != null ? BaseWorkplaceList[baseWorkplaceCbx.SelectedIndex].Key : 0;
            if (alternativeWorkplaceCbx.SelectedItem != null && AlternativeWorkplaceList[alternativeWorkplaceCbx.SelectedIndex].Key > 0)
                workplace = AlternativeWorkplaceList[alternativeWorkplaceCbx.SelectedIndex].Key;
            startTimeDtp.Value = PlanCombin.GetStartTime(Data, CombinItemsFirstTask, workplace);
        }
    }
}

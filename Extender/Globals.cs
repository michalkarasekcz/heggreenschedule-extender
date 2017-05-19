using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Noris.Schedule.Planning.DataFace;
using Noris.Schedule.Planning.ProcessData;
using Noris.Schedule.Support;
using Noris.Schedule.Support.Services;
using Noris.WS.ServiceGate;

namespace Noris.Schedule.Extender
{
    class Globals
    {
        /// <summary>
        /// Spuštění funkce v HeG
        /// </summary>
        /// <param name="classNumber">Číslo třídy funkce</param>
        /// <param name="functionShortName">Krátký název funkce</param>
        /// <param name="recordNumbers">Záznamy, nad kterými má být funkce spuštěna</param>
        /// <returns>Vrací true, jestli funkce doběhla úspěšně, false jestli neúspěšně</returns>
        public static bool RunHeGFunction(int classNumber, string functionShortName, List<int> recordNumbers)
        {
            bool result = false;
            try
            {
                if (Steward.HaveCurrentUserPassword())
                {

                    string mess;
                    Noris.WS.ServiceGate.RunFunctionResponse response = Steward.ServiceGateAdapter.RunFunction(classNumber, functionShortName, recordNumbers);
                    if (response.Auditlog == null)
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
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                result = false;
            }
            return result;
        }
    }
}

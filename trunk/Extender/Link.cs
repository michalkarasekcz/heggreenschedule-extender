using System;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Collections.Generic;
using System.Text;

using Noris.Schedule.Support.Data;
using Noris.Schedule.Support.Sql;

namespace Noris.Schedule.Extender
{
    #region CLASS LinkCls : Třída odvozená z tabulky lcs.link
    /// <summary>
    /// LinkCls : Data z Master tabulky třídy 22300: Spojovací záznam operací (LinkInst), Subjektová
    /// Data načtena z tabulky lcs.link
    /// </summary>
    public partial class LinkCls : RecordCls, Noris.Schedule.Support.Sql.IDbRecord
    {
        #region KONSTRUKTORY
        /// <summary>
        /// Implicitní konstruktor, zajistí číslování záznamů do jednotné Temporary řady
        /// </summary>
        public LinkCls()
            : base()
        { }
        /// <summary>
        /// Konstruktor s načítáním dat z databáze
        /// </summary>
        /// <param name="recordNumber">Číslo záznamu, který je třeba načíst</param>
        public LinkCls(int recordNumber)
            : base(recordNumber)
        { }
        /// <summary>
        /// Explicitní konstruktor, dovolí zvolit, zda se pro tento záznam bude generovat Temporary ID
        /// </summary>
        /// <param name="createTemporaryId">true pokud se pro záznam má vytvořit Temporary ID.
        /// Default (false) = nevytvoří se, bude mít RecordNumber = 0.</param>
        public LinkCls(bool createTemporaryId)
            : base(createTemporaryId)
        { }
        #endregion
        #region PUBLIC OVERRIDE PROPERTY
        /// <summary>Číslo třídy (22300)</summary>
        public override int ClassNumber { get { return ClassNr; } }
        /// <summary>Číslo třídy (22300)</summary>
        public const int ClassNr = 22300;
        /// <summary>Číslo záznamu = cislo_subjektu</summary>
        public override int RecordNumber { get { return base._cislo_subjektu; } }
        /// <summary>IDbRecord.RecordNumber : musí konkrétně implementovat set, protože DbLayer po uložení nového záznamu (kde RecordNumber == 0) do něj vloží přidělený klíč</summary>
        int IDbRecord.RecordNumber
        {
            get { return this.RecordNumber; }
            set { base._cislo_subjektu = value; }
        }
        #endregion
        #region PUBLIC DATA PROPERTY
        ///<summary><para>Systémový atribut: reference subjektu</para><para>Db: lcs.subjekty.reference_subjektu (varchar (30) null)</para></summary>
        public new string Reference { get { return _reference_subjektu; } set { _reference_subjektu = value; } }
        ///<summary><para>Systémový atribut: název subjektu</para><para>Db: lcs.subjekty.nazev_subjektu (varchar (40) null)</para></summary>
        public new string Nazev { get { return _nazev_subjektu; } set { _nazev_subjektu = value; } }
        ///<summary><para>Systémový atribut: číslo pořadače subjektu</para><para>Db: lcs.subjekty.cislo_poradace (int not null)</para></summary>
        public new int FolderNumber { get { return _cislo_poradace; } set { _cislo_poradace = value; } }
        ///<summary><para>Db: lcs.link.obdobi (bigint null)</para></summary>
        public SqlInt64 Obdobi { get { return _obdobi; } set { _obdobi = value; } }
        ///<summary><para>Vztah 22944: Konkrétní kombinace výlisků (zprava: Spojovací záznam operací)</para><para>Db: lcs.link.press_fact_combin (int null)</para></summary>
        public SqlInt32 PressFactCombin { get { return _press_fact_combin; } set { _press_fact_combin = value; } }
        #endregion
        #region PROTECTED DATA FIELDS
        protected SqlInt64 _obdobi;
        protected SqlInt32 _press_fact_combin;
        #endregion
        #region FILL FROM READER
        /// <summary>
        /// Virtuální metoda, která umožňuje potomkům provádět typovou a rychlou konverzi dat proudících z databáze přes DataReader 
        /// přímo do objektu této třídy, bez použití generické metody FieldInfo.SetValue (která je poněkud pomalá).
        /// Tato metoda v této třídě tedy má naplnit každou svoji instanční proměnnou (field) hodnotou z předaného SqlDataReaderu.
        /// Pro snadné načtení dat je předán objekt mapper, který nabízí svou generickou metodu FillDataIntoField«int»(string dbColumnName, SqlDataReader reader).
        /// Tato metoda vrátí načtená data (typovaná jako objekt).
        /// Čtení touto metodou je cca 6x rychlejší než čtení generickou metodou.
        /// Metoda má vracet true, pokud převzala všechna data. 
        /// Je nanejvýše účelné, aby tak provedla, protože pokud vrátí false, pak se po jejím skončení provede generické ukládání dat (což je 6x pomalejší).
        /// </summary>
        /// <param name="reader">Vstupní DataReader</param>
        /// <param name="map">Mapování dat z readeru na fields tohoto objektu</param>
        /// <returns>true = data byla převzata / false = data nepřevzata</returns>
        public override bool FillFromReader(SqlDataReader reader, FieldMapperLoad map)
        {
            base.FillFromReader(reader, map);
            _obdobi = (SqlInt64)map.FillDataIntoField<SqlInt64>("obdobi", reader);
            _press_fact_combin = (SqlInt32)map.FillDataIntoField<SqlInt32>("press_fact_combin", reader);
            return true;
        }
        #endregion
        #region SAVE RECORD
        /// <summary>
        /// Zajistí přípravu svých dat do podkladové vrstvy pro uložení do databáze.
        /// Subjektové třídy musí nejprve volat SubjectCls.PrepareSaveData(this, map);, 
        /// tím se uloží data do tabulky subjektů (a pro nové záznamy se vygeneruje číslo subjektu).
        /// Nonsubjektové třídy to volat nesmí.
        /// </summary>
        /// <param name="map"></param>
        /// <returns></returns>
        public override bool PrepareSaveData(FieldMapperSave map)
        {
            SubjectCls.PrepareSaveData(this, map);
            SubjectCls.PrepareSaveData(this, map, "lcs.link");
            map.AddColumn("obdobi", this.Obdobi);
            map.AddColumn("press_fact_combin", this.PressFactCombin);
            map.AddTableEnd();
            return true;
        }
        #endregion
        #region STATIC GENERÁTORY SELECTU
        /// <summary>
        /// SQL SELECT pro načtení columnů, odpovídajících této struktuře. Pozor: neobsahuje filtr ani order.
        /// </summary>
        public static new string SqlSelect { get { return @"SELECT * FROM lcs.link"; } }
        /// <summary>
        /// Jméno sloupce, který obsahuje klíč záznamu, určeno pro filtry.
        /// </summary>
        public static new string SqlRecord { get { return "cislo_subjektu"; } }
        /// <summary>
        /// Generátor SELECTU pro načítání do CACHE.
        /// Vrátí SQL SELECT, který používá SysCache pro automatické načítání záznamů do cache.
        /// Vrácený select má tvar "SELECT * FROM tabulka [join lcs.subjekty] WHERE [master_tabulka.]key_column", cache si doplní číslo[a] záznamů sama.
        /// </summary>
        /// <returns>Vrátí SQL SELECT, který používá SysCache pro automatické načítání záznamů do cache.</returns>
        public override string GetCacheSelect()
        {
            return SqlSelect + " WHERE " + SqlRecord;
        }
        #endregion
    }
    #endregion
}

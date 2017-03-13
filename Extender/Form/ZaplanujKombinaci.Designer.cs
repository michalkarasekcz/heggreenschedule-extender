namespace Noris.Schedule.Extender
{
    partial class ZaplanujKombinaci
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.lqtyLbl = new System.Windows.Forms.Label();
            this.qtyTbx = new System.Windows.Forms.TextBox();
            this.okBtn = new System.Windows.Forms.Button();
            this.stornoBtn = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.alternativeWorkplaceCbx = new System.Windows.Forms.ComboBox();
            this.startTimeDtp = new System.Windows.Forms.DateTimePicker();
            this.label3 = new System.Windows.Forms.Label();
            this.baseWorkplaceCbx = new System.Windows.Forms.ComboBox();
            this.SuspendLayout();
            // 
            // lqtyLbl
            // 
            this.lqtyLbl.AutoSize = true;
            this.lqtyLbl.Location = new System.Drawing.Point(25, 22);
            this.lqtyLbl.Name = "lqtyLbl";
            this.lqtyLbl.Size = new System.Drawing.Size(67, 13);
            this.lqtyLbl.TabIndex = 0;
            this.lqtyLbl.Text = "Počet zálisů:";
            // 
            // qtyTbx
            // 
            this.qtyTbx.Location = new System.Drawing.Point(191, 19);
            this.qtyTbx.Name = "qtyTbx";
            this.qtyTbx.Size = new System.Drawing.Size(286, 20);
            this.qtyTbx.TabIndex = 1;
            // 
            // okBtn
            // 
            this.okBtn.Location = new System.Drawing.Point(28, 141);
            this.okBtn.Name = "okBtn";
            this.okBtn.Size = new System.Drawing.Size(75, 23);
            this.okBtn.TabIndex = 2;
            this.okBtn.Text = "OK";
            this.okBtn.UseVisualStyleBackColor = true;
            this.okBtn.Click += new System.EventHandler(this._Validate);
            // 
            // stornoBtn
            // 
            this.stornoBtn.Location = new System.Drawing.Point(402, 141);
            this.stornoBtn.Name = "stornoBtn";
            this.stornoBtn.Size = new System.Drawing.Size(75, 23);
            this.stornoBtn.TabIndex = 3;
            this.stornoBtn.Text = "Storno";
            this.stornoBtn.UseVisualStyleBackColor = true;
            this.stornoBtn.Click += new System.EventHandler(this._NoValidate);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(25, 54);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(80, 13);
            this.label1.TabIndex = 5;
            this.label1.Text = "Počáteční čas:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(25, 116);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(116, 13);
            this.label2.TabIndex = 6;
            this.label2.Text = "Alternativní pracoviště:";
            // 
            // alternativeWorkplaceCbx
            // 
            this.alternativeWorkplaceCbx.FormattingEnabled = true;
            this.alternativeWorkplaceCbx.Location = new System.Drawing.Point(191, 113);
            this.alternativeWorkplaceCbx.Name = "alternativeWorkplaceCbx";
            this.alternativeWorkplaceCbx.Size = new System.Drawing.Size(286, 21);
            this.alternativeWorkplaceCbx.TabIndex = 8;
            this.alternativeWorkplaceCbx.SelectedIndexChanged += new System.EventHandler(this._FillStartTime);
            // 
            // startTimeDtp
            // 
            this.startTimeDtp.CustomFormat = " dd.MM.yyyy HH:mm";
            this.startTimeDtp.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.startTimeDtp.Location = new System.Drawing.Point(191, 50);
            this.startTimeDtp.MinDate = new System.DateTime(1900, 1, 1, 0, 0, 0, 0);
            this.startTimeDtp.Name = "startTimeDtp";
            this.startTimeDtp.Size = new System.Drawing.Size(286, 20);
            this.startTimeDtp.TabIndex = 9;
            this.startTimeDtp.Value = new System.DateTime(2010, 12, 29, 8, 30, 0, 0);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(25, 84);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(105, 13);
            this.label3.TabIndex = 10;
            this.label3.Text = "Základní pracoviště:";
            // 
            // baseWorkplaceCbx
            // 
            this.baseWorkplaceCbx.FormattingEnabled = true;
            this.baseWorkplaceCbx.Location = new System.Drawing.Point(191, 81);
            this.baseWorkplaceCbx.Name = "baseWorkplaceCbx";
            this.baseWorkplaceCbx.Size = new System.Drawing.Size(286, 21);
            this.baseWorkplaceCbx.TabIndex = 13;
            // 
            // ZaplanujKombinaci
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(522, 190);
            this.ControlBox = false;
            this.Controls.Add(this.baseWorkplaceCbx);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.startTimeDtp);
            this.Controls.Add(this.alternativeWorkplaceCbx);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.stornoBtn);
            this.Controls.Add(this.okBtn);
            this.Controls.Add(this.qtyTbx);
            this.Controls.Add(this.lqtyLbl);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ZaplanujKombinaci";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Zaplánuj kombinaci";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this._ReturnParams);
            this.Load += new System.EventHandler(this._FillParams);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lqtyLbl;
        private System.Windows.Forms.TextBox qtyTbx;
        private System.Windows.Forms.Button okBtn;
        private System.Windows.Forms.Button stornoBtn;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox alternativeWorkplaceCbx;
        private System.Windows.Forms.DateTimePicker startTimeDtp;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.ComboBox baseWorkplaceCbx;
    }
}
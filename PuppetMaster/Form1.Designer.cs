namespace PubSub
{
    partial class Form1
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
            this.file_Txb = new System.Windows.Forms.TextBox();
            this.script_Txb = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // file_Txb
            // 
            this.file_Txb.Location = new System.Drawing.Point(12, 29);
            this.file_Txb.Name = "file_Txb";
            this.file_Txb.Size = new System.Drawing.Size(488, 20);
            this.file_Txb.TabIndex = 0;
            this.file_Txb.KeyUp += new System.Windows.Forms.KeyEventHandler(this.file_Txb_KeyUp);
            // 
            // script_Txb
            // 
            this.script_Txb.Location = new System.Drawing.Point(12, 70);
            this.script_Txb.Name = "script_Txb";
            this.script_Txb.Size = new System.Drawing.Size(488, 20);
            this.script_Txb.TabIndex = 1;
            this.script_Txb.KeyUp += new System.Windows.Forms.KeyEventHandler(this.script_Txb_KeyUp);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(13, 13);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(53, 13);
            this.label1.TabIndex = 2;
            this.label1.Text = "Config file";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(512, 205);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.script_Txb);
            this.Controls.Add(this.file_Txb);
            this.Name = "Form1";
            this.Text = "Form1";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox file_Txb;
        private System.Windows.Forms.TextBox script_Txb;
        private System.Windows.Forms.Label label1;
    }
}

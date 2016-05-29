namespace TawGatherMembersInfo
{
    partial class EnterTawUserLoginInfo
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(EnterTawUserLoginInfo));
            this.cancel = new System.Windows.Forms.Button();
            this.login = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.rememeberLoginDetails = new System.Windows.Forms.CheckBox();
            this.username = new System.Windows.Forms.TextBox();
            this.password = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // cancel
            // 
            this.cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cancel.Location = new System.Drawing.Point(389, 66);
            this.cancel.Name = "cancel";
            this.cancel.Size = new System.Drawing.Size(105, 23);
            this.cancel.TabIndex = 0;
            this.cancel.Text = "Cancel";
            this.cancel.UseVisualStyleBackColor = true;
            this.cancel.Click += new System.EventHandler(this.cancel_Click);
            // 
            // login
            // 
            this.login.Location = new System.Drawing.Point(389, 92);
            this.login.Name = "login";
            this.login.Size = new System.Drawing.Size(105, 23);
            this.login.TabIndex = 1;
            this.login.Text = "Login";
            this.login.UseVisualStyleBackColor = true;
            this.login.Click += new System.EventHandler(this.login_Click);
            // 
            // label1
            // 
            this.label1.AutoEllipsis = true;
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(7, 8);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(419, 52);
            this.label1.TabIndex = 2;
            this.label1.Text = resources.GetString("label1.Text");
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(6, 71);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(102, 13);
            this.label2.TabIndex = 3;
            this.label2.Text = "TAW.net username:";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(6, 97);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(101, 13);
            this.label3.TabIndex = 4;
            this.label3.Text = "TAW.net password:";
            // 
            // rememeberLoginDetails
            // 
            this.rememeberLoginDetails.AutoSize = true;
            this.rememeberLoginDetails.Checked = true;
            this.rememeberLoginDetails.CheckState = System.Windows.Forms.CheckState.Checked;
            this.rememeberLoginDetails.Location = new System.Drawing.Point(114, 120);
            this.rememeberLoginDetails.Name = "rememeberLoginDetails";
            this.rememeberLoginDetails.Size = new System.Drawing.Size(141, 17);
            this.rememeberLoginDetails.TabIndex = 5;
            this.rememeberLoginDetails.Text = "Rememeber login details";
            this.rememeberLoginDetails.UseVisualStyleBackColor = true;
            // 
            // username
            // 
            this.username.Location = new System.Drawing.Point(114, 68);
            this.username.Name = "username";
            this.username.Size = new System.Drawing.Size(270, 20);
            this.username.TabIndex = 6;
            // 
            // password
            // 
            this.password.Location = new System.Drawing.Point(114, 94);
            this.password.Name = "password";
            this.password.PasswordChar = '*';
            this.password.Size = new System.Drawing.Size(270, 20);
            this.password.TabIndex = 7;
            // 
            // EnterTawUserLoginInfo
            // 
            this.AcceptButton = this.login;
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.CancelButton = this.cancel;
            this.ClientSize = new System.Drawing.Size(500, 145);
            this.Controls.Add(this.password);
            this.Controls.Add(this.username);
            this.Controls.Add(this.rememeberLoginDetails);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.login);
            this.Controls.Add(this.cancel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "EnterTawUserLoginInfo";
            this.ShowIcon = false;
            this.Text = "Please enter TAW.net user login details";
            this.TopMost = true;
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button cancel;
        private System.Windows.Forms.Button login;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.CheckBox rememeberLoginDetails;
        private System.Windows.Forms.TextBox username;
        private System.Windows.Forms.TextBox password;
    }
}
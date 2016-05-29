using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TawGatherMembersInfo
{
    public partial class EnterTawUserLoginInfo: Form
    {
        public string Username
        {
            get
            {
                return username.Text;
            }
        }
        public string Password
        {
            get
            {
                return password.Text;
            }
        }

        public bool RememeberLoginDetails
        {
            get
            {
                return rememeberLoginDetails.Checked;
            }
        }

        public EnterTawUserLoginInfo()
        {
            InitializeComponent();
        }
        private void cancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
        private void login_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}

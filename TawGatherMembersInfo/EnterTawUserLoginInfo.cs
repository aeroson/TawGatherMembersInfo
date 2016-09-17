using System;
using System.Windows.Forms;

namespace TawGatherMembersInfo
{
	public partial class EnterTawUserLoginInfo : Form
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

		void cancel_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
			Close();
		}

		void login_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.OK;
			Close();
		}
	}
}
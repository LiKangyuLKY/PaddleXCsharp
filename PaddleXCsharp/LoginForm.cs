using System;
using System.Windows.Forms;

namespace PaddleXCsharp
{
    public partial class LoginForm : Form
    {
        public LoginForm()
        {
            InitializeComponent();
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            if (radioButton1.Checked)
            {
                this.Hide();
                SingleCamera singleCamera = new SingleCamera();
                singleCamera.Show();
            }
            else if (radioButton2.Checked)
            {
                this.Hide();
                DoubleCamera doubleCamera = new DoubleCamera();
                doubleCamera.Show();
            }
            else if (radioButton3.Checked)
            {
                MessageBox.Show("还没做呢！");
            }
        }

        private void LoginForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            System.Environment.Exit(0);
        }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Interface_KM
{
    public partial class Form1 : Form
    {
        List<Panel> listpanel = new List<Panel>();
        int index;
        int count;
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

           // listpanel[index].BringToFront();
        }

        private void btn_I_Click(object sender, EventArgs e)
        {
            strUnit.Text = "A";

            str_RR.Visible = true;
            str_SS.Visible = true;
            str_TT.Visible = true;

            str_RS.Visible = false;
            str_ST.Visible = false;
            str_TR.Visible = false;

            strR.Visible = true;
            strS.Visible = true;
            strT.Visible = true;
        }

        private void btn_1ph_Click(object sender, EventArgs e)
        {
            strUnit.Text = "V";

            str_RR.Visible = true;
            str_SS.Visible = true;
            str_TT.Visible = true;

            str_RS.Visible = false;
            str_ST.Visible = false;
            str_TR.Visible = false;

            strR.Visible = true;
            strS.Visible = true;
            strT.Visible = true;
        }

        private void btn_3ph_Click(object sender, EventArgs e)
        {
            strUnit.Text = "V";

            str_RR.Visible = true;
            str_SS.Visible = true;
            str_TT.Visible = true;

            str_RS.Visible = true;
            str_ST.Visible = true;
            str_TR.Visible = true;

            strR.Visible = true;
            strS.Visible = true;
            strT.Visible = true;
        }

        private void btn_f_Click(object sender, EventArgs e)
        {
            strUnit.Text = "Hz";

            str_RR.Visible = false;
            str_SS.Visible = false;
            str_TT.Visible = false;

            str_RS.Visible = false;
            str_ST.Visible = false;
            str_TR.Visible = false;

            /*---*/
            strR.Visible = true;
            strS.Visible = false;
            strT.Visible = false;
        }

        private void btn_pf_Click(object sender, EventArgs e)
        {
            strUnit.Text = "PF";

            str_RR.Visible = false;
            str_SS.Visible = false;
            str_TT.Visible = false;

            str_RS.Visible = false;
            str_ST.Visible = false;
            str_TR.Visible = false;

            /*---*/
            strR.Visible = true;
            strS.Visible = false;
            strT.Visible = false;
        }

        private void btn_p_Click(object sender, EventArgs e)
        {
            strUnit.Text = "kW";

            str_RR.Visible = false;
            str_SS.Visible = false;
            str_TT.Visible = false;

            str_RS.Visible = false;
            str_ST.Visible = false;
            str_TR.Visible = false;

            /*---*/
            strR.Visible = true;
            strS.Visible = false;
            strT.Visible = false;
        }

        private void btn_q_Click(object sender, EventArgs e)
        {
            strUnit.Text = "kvar";

            str_RR.Visible = false;
            str_SS.Visible = false;
            str_TT.Visible = false;

            str_RS.Visible = false;
            str_ST.Visible = false;
            str_TR.Visible = false;

            /*---*/
            strR.Visible = true;
            strS.Visible = false;
            strT.Visible = false;
        }

        private void button1_Click(object sender, EventArgs e)
        {

            //  panel_2.SendToBack();
            // panel_2.Visible = false;
            // this.panel_1.BringToFront();
        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton1.Checked == true)
            {
                panel_1.Show();
                panel_2.Hide();
                //listpanel[0].BringToFront();
                //panel_1.BringToFront();
               // panel_1.Visible = true;
                //panel_2.Visible = false;
            }
                
        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton2.Checked == true)
            {
                panel_1.Hide();
                panel_2.Show();
                //listpanel[1].BringToFront();
                //panel_1.Visible = false;
                //panel_2.Visible = true;
               // panel_2.BringToFront();
               // panel_1.SendToBack();
            }
            // panel_2.BringToFront();
        }
    }
}

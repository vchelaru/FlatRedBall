using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace FlatRedBall.Glue.Controls
{
	public partial class SelectExistingBehaviorWindow : Form
	{
		public string Result
		{
			get
			{
				return dataGridView1.SelectedRows[0].Cells[0].Value as string;
			}
		}

		public SelectExistingBehaviorWindow()
		{
			InitializeComponent();
		}

		private void SelectExistingBehaviorWindow_Load(object sender, EventArgs e)
		{
			List<string> allBehaviors = BehaviorManager.GetAllBehaviors();

			dataGridView1.ColumnCount = 2;

			dataGridView1.Columns[0].Name = "Behavior";
			dataGridView1.Columns[1].Name = "Detail";

			for (int i = 0; i < allBehaviors.Count; i++)
			{
				dataGridView1.Rows.Add();


				dataGridView1.Rows[i].Cells[0].Value = allBehaviors[i];
				dataGridView1.Rows[i].Cells[1].Value = "";
				//string[] strings = new string[]{
				//	"test" + i,
				//	FlatRedBallServices.Random.Next(10).ToString()};
				//dataGridView1.Rows.Add(strings);

				//string[] strings = new string[]{
				//    allBehaviors[i],
				//    FlatRedBallServices.Random.Next(10).ToString()};

				//dataGridView1.Rows.Add(strings);
			}

		}
	}
}

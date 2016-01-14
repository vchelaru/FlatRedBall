using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlatRedBall.Glue.SaveClasses
{
	public class FileAssociationsSave
	{
		public List<string> AvailableApplications = new List<string>();

		public List<string> Extensions = new List<string>();
		public List<string> AssociatedApplications = new List<string>();
	}
}

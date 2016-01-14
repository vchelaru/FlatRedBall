using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
//using FlatRedBall.Glue.Settings;

namespace FlatRedBall.Glue.GuiDisplay
{
	public class AvailableApplicationsStringConverters : TypeConverter
	{
		public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
		{
			return true;
		}

		public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
		{
			return true;
		}

		List<string> stringToReturn = new List<string>();
		public override StandardValuesCollection
					 GetStandardValues(ITypeDescriptorContext context)
		{
			stringToReturn.Clear();
            stringToReturn.AddRange(Facades.FacadeContainer.Self.ApplicationSettings.AvailableApplications);//  EditorData.FileAssociationSettings.AvailableApplications);
			stringToReturn.Add("New Application...");


			StandardValuesCollection svc = new StandardValuesCollection(stringToReturn);

			return svc;
		} 


	}
}

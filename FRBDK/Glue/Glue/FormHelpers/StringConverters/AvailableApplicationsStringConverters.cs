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

		public override StandardValuesCollection
					 GetStandardValues(ITypeDescriptorContext context)
		{
			return new StandardValuesCollection(EditorProperties.ReferencedFileSavePropertyModel
				.GetApplicationOptions(Facades.FacadeContainer.Self.ApplicationSettings).ToList());
		}


	}
}

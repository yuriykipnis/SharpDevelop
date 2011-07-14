using System;
using System.Reflection;
using ICSharpCode.Core;

namespace Internal.Doozers
{
	public class ToolContentDoozer : IDoozer
	{
		private String _name;
		private String _className;
		private String _holder;
		
		public bool HandleConditions
		{
			get{ return false; }
		}
		
		public object BuildItem(object caller, Codon codon, System.Collections.ArrayList subItems)
		{
			_name = codon.Id;
			_className = codon.Properties["class"];
			_holder = codon.Properties["holder"];
			Type singletonType = codon.AddIn.FindType(_className);
			
			if (singletonType != null)
			{
				PropertyInfo getInstance = singletonType.GetProperty("Instance");
				if (getInstance == null)
				{
					LoggingService.Error(String.Format("Tool content {0} related to {1} view content, should be a singelton class", 
						singletonType, _holder));
					return null;
				}
				return getInstance.GetValue(null, null);
			}
			
			LoggingService.Error(String.Format("Class {0} was not foound", _className));
			return null;
		}
		
		public override string ToString()
		{
			return String.Format("[ToolContentDoozer : className = {0}, name = {1}]", _className, _name);
		}
	}
}

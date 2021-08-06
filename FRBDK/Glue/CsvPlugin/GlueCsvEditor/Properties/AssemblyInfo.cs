using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("GlueCsvEditor")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Microsoft")]
[assembly: AssemblyProduct("GlueCsvEditor")]
[assembly: AssemblyCopyright("Copyright © Microsoft 2011")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("f92cda9e-871a-4aa5-92f7-90defc1f4ea4")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Build and Revision Numbers 
// by using the '*' as shown below:
// [assembly: AssemblyVersion("1.0.*")]
// 1.12.* - Added CTRL + enter to add new row
// 1.13.* 
// - Added CTRL + SHIFT + enter to insert row above current row
// - Updated to latest MSBuild to work with latest Glue
// - Added tips to right-click menu so users know about CTRL + (shift?) enter
// 1.14.*
// - Fixed crash that can occur when the plugin reloads if the CSV file is deleted.
// 1.14.1
// - Updated to latest Newtonsoft Json (FRB Glue requirement)
[assembly: AssemblyVersion("1.14.1.*")]

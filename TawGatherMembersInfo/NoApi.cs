using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TawGatherMembersInfo
{
	/// <summary>
	/// Marks property or field that should not be accessible with web api.
	/// </summary>
	public class NoApi : Attribute
	{
	}
}
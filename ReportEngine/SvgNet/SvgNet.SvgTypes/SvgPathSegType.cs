using System;

namespace SvgNet.SvgTypes
{
	public enum SvgPathSegType
	{
		SVG_SEGTYPE_UNKNOWN,
		SVG_SEGTYPE_MOVETO,
		SVG_SEGTYPE_CLOSEPATH,
		SVG_SEGTYPE_LINETO,
		SVG_SEGTYPE_HLINETO,
		SVG_SEGTYPE_VLINETO,
		SVG_SEGTYPE_CURVETO,
		SVG_SEGTYPE_SMOOTHCURVETO,
		SVG_SEGTYPE_BEZIERTO,
		SVG_SEGTYPE_SMOOTHBEZIERTO,
		SVG_SEGTYPE_ARCTO
	}
}
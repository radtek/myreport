﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECharts.Entities.axis
{
	public class ChartAxis<T> : Axis where T : class
	{
		public AxisType? type { get; set; }

		public bool? show { get; set; }

		public int? zlevel { get; set; }

		public int? z { get; set; }

		public string name { get; set; }

		public NameLocationType? nameLocation { get; set; }

		public string position { get; set; }

		public int? splitNumber { get; set; }


		public double? min { get; set; }

		public dynamic max { get; set; }

		public bool? scale { get; set; }

		public T Max (dynamic max)
		{
			this.max = max;
			return this as T;
		}

		public T Min (double min)
		{
			this.min = min;
			return this as T;
		}

		public T Scale (bool scale)
		{
			this.scale = scale;
			return this as T;
		}

		public T Show (bool show)
		{
			this.show = show;
			return this as T;
		}

		public T Zlevel (int zlevel)
		{
			this.zlevel = zlevel;
			return this as T;
		}

		public T Z (int z)
		{
			this.z = z;
			return this as T;
		}

		public T Name (int splitNumber)
		{
			this.name = name;
			return this as T;
		}

		public T Position (PositionType p)
		{
			this.position = p.ToString ();
			return this as T;
		}

		public T NameLocation (NameLocationType nameLocation)
		{
			this.nameLocation = nameLocation;
			return this as T;
		}


		public T Name (string name)
		{
			this.name = name;
			return this as T;
		}

		public T SplitNumber (int splitNumber)
		{
			this.splitNumber = splitNumber;
			return this as T;
		}



	}
}

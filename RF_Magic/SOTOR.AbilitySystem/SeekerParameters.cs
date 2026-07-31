using System;
using System.Xml.Serialization;

namespace SOTOR.AbilitySystem;

[Serializable]
public class SeekerParameters
{
	[XmlAttribute]
	public float Proportional = 0.5f;

	[XmlAttribute]
	public float Derivative;

	[XmlAttribute]
	public float MaxDistance = float.MaxValue;

	[XmlAttribute]
	public float MinDistance = float.MinValue;

	[XmlAttribute]
	public float DisableDistance = float.MinValue;
}

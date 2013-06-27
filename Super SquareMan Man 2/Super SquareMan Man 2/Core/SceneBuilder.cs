using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SSMM2.Core
{
	public struct EntityConstructionData
	{
		public Color BlendColor;
		public Vector2 Position;
		public Vector2 Scale;
		public float Angle;

		public Dictionary<String, object> CustomProperties;
	}

	public interface BuilderComponent
	{
		String [] CorrespondingResourceNames
		{
			get;
		}

		void BuildObject(String resourceName, Scene targetScene, EntityConstructionData constructionData);
	}

	public class SceneBuilder
	{
		public static List<BuilderComponent> Components;

		public static String LastScene = "";

		public static void BuildScene(String sourceFile, Scene targetScene)
		{
			LastScene = sourceFile;

			XmlDocument levelData = new XmlDocument();
			if (!File.Exists(sourceFile)) throw new Exception("Unable to find level file.");
			levelData.Load(sourceFile);

			XmlNodeList levelComponents = levelData.GetElementsByTagName("Item");
			foreach (XmlNode node in levelComponents)
			{
				XmlAttribute nodeType = node.Attributes["xsi:type"];
				if (
					nodeType != null &&
					nodeType.Value != "TextureItem" &&
					nodeType.Value != "RectangleItem"
					)
					throw new Exception ("Unrecognized resource type " + nodeType.Value);

				String componentType = nodeType.Value;

				String resourceName;

				EntityConstructionData entityData = new EntityConstructionData();

				var customPropertiesNode = node["CustomProperties"];
				entityData.CustomProperties = new Dictionary<String, object>();
				foreach (XmlNode currentProperty in customPropertiesNode)
				{
					String propertyName = currentProperty.Attributes["Name"].InnerText;
					String propertyType = currentProperty.Attributes["Type"].InnerText;

					switch (propertyType.ToLower())
					{
						case "bool":
							{
								entityData.CustomProperties[propertyName] = new Nullable<bool> (bool.Parse(currentProperty["boolean"].InnerText));
								break;
							}

						case "string":
							{
								entityData.CustomProperties[propertyName] = currentProperty["string"].InnerText;
								break;
							}

						default:
							throw new Exception("Unable to process unknown attribute type " + propertyType);
					}
				}

				switch (componentType)
				{
					case ("TextureItem"):
						{
							entityData.Angle = float.Parse(node["Rotation"].InnerText);

							entityData.BlendColor = new Color();
							entityData.BlendColor.R = byte.Parse(node["TintColor"]["R"].InnerText);
							entityData.BlendColor.G = byte.Parse(node["TintColor"]["G"].InnerText);
							entityData.BlendColor.B = byte.Parse(node["TintColor"]["B"].InnerText);
							entityData.BlendColor.A = byte.Parse(node["TintColor"]["A"].InnerText);

							entityData.Scale = new Vector2(
								float.Parse(node["Scale"]["X"].InnerText),
								float.Parse(node["Scale"]["Y"].InnerText)
								);

							entityData.Position = new Vector2(
								float.Parse(node["Position"]["X"].InnerText),
								float.Parse(node["Position"]["Y"].InnerText)
								);

							resourceName = Path.GetFileName(node["asset_name"].InnerText);
							ResourceManager.Instance.PreCacheResource<Texture2D>(node["asset_name"].InnerText);

							break;
						}

					case ("RectangleItem"):
						{
							entityData.Angle = 0.0F;

							entityData.BlendColor = new Color();
							entityData.BlendColor.R = byte.Parse(node["FillColor"]["R"].InnerText);
							entityData.BlendColor.G = byte.Parse(node["FillColor"]["G"].InnerText);
							entityData.BlendColor.B = byte.Parse(node["FillColor"]["B"].InnerText);
							entityData.BlendColor.A = byte.Parse(node["FillColor"]["A"].InnerText);


							entityData.Position = new Vector2(
								float.Parse(node["Position"]["X"].InnerText),
								float.Parse(node["Position"]["Y"].InnerText)
								);

							entityData.Scale = new Vector2(
								float.Parse(node["Width"].InnerText),
								float.Parse(node["Height"].InnerText)
								);

							resourceName = "$Rectangle";

							break;
						}

					default:
						{
							resourceName = "";
							break;
						}
				}

				foreach (BuilderComponent builder in Components)
				{
					foreach (String relevantAsset in builder.CorrespondingResourceNames)
					{
						if (relevantAsset == resourceName)
						{
							builder.BuildObject(resourceName, targetScene, entityData);
						}
					}
				}
			}

			targetScene.Source = sourceFile;
			targetScene.SourceType = Scene.SceneSourceType.File;
		}
	}
}

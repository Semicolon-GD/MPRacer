using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(menuName = "Car Definition")]
public class CarDefinition : ScriptableObject
{
   public Sprite FrontShot;
   public Sprite SideShot;
   public GameObject Prefab;
   public string Name;

   public static CarDefinition GetCarDefinition(string name)
   {
      var allDefinitions = Resources.LoadAll<CarDefinition>("CarDefinitions");
      foreach (var definition in allDefinitions)
      {
         if (string.Compare(definition.name, name, StringComparison.InvariantCultureIgnoreCase) == 0)
            return definition;
      }

      return null;
   }
}

/*
 *    (C) Copyright 2013 Olivier Battini (https://olivierbattini.fr)
 *
 *    ALL RIGHTS RESERVED
 *
 *    This file is only published for showcase purposes and use in source and
 *    binary forms, with or without modification, are not permitted in any way.
 */

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Timers;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
using System.Globalization;
using System.Threading;
using log4net;
using Nini.Config;
using Mono.Addins;

/*

    Some references removed

*/

namespace TesseractPhysics
{
    public class TesseractPhysics : INonSharedRegionModule
    {
        /*

            Some code removed

        */

        private void UpdateSimulation()
        {
            lock (m_GSObjects)
            {
                // Objects to be removed
                List<string> removedObjects = new List<string>();


                // Update loop for each simulation object
                foreach (GSObject currentGso in m_GSObjects.Values)
                {
                    Vector3 globalForce = new Vector3(0, 0, 0);

                    // Computing force applied by each neighbour object
                    // and adding it to the current object globalForce
                    foreach (GSObject neighbourGso in m_GSObjects.Values)
                    {
                        if (currentGso.Uuid != neighbourGso.Uuid)
                        {
                            // F = G * M1 * M2 / d²
                            float distance = m_DConstant * Vector3.Distance(currentGso.CurrentPosition, neighbourGso.CurrentPosition);
                            // Added distance minimum value to avoid 
                            // if (distance < m_DConstant) { distance = m_DConstant * 10; }
                            float force = m_GConstant * currentGso.Mass * neighbourGso.Mass / (float)Math.Pow(distance, 2);
                            Vector3 normalVect = Vector3.Normalize(neighbourGso.CurrentPosition - currentGso.CurrentPosition);

                            globalForce += normalVect * force;
                        }
                    }

                    // Computing acceleration A = F / M
                    Vector3 acceleration = globalForce / currentGso.Mass;
                    float duration = (float)m_TimerDelay;

                    // New velocity V = A * t
                    currentGso.NewVelocity = currentGso.CurrentVelocity + acceleration * duration;
                    currentGso.NewPosition = currentGso.CurrentPosition + currentGso.CurrentVelocity * duration + acceleration * (float)Math.Pow(duration, 2);

                    // Checking region borders
                    if (0 >= currentGso.NewPosition.X | currentGso.NewPosition.X >= 256 |
                        0 >= currentGso.NewPosition.Y | currentGso.NewPosition.Y >= 256)
                    {
                        removedObjects.Add(currentGso.Name);
                    }
                    else
                    {
                        currentGso.ApplyAndUpdate();
                    }
                }

                foreach (string gsoName in removedObjects)
                    RemoveObjectFromSimulation(m_GSObjects[gsoName].SceneObject, false);
            }

            /*

                Some code removed

            */
        }
    }
}
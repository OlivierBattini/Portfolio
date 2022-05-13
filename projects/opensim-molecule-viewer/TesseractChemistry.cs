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
using log4net;
using Nini.Config;

using System.Xml;
using System.Xml.Serialization;
using System.IO;
using System.Globalization;
using System.Threading;

using OpenBabelServer;
using OpenBabelServer.Service;
using System.ServiceModel;
using System.Linq;
using System.Net;
using System.Web;
using Mono.Addins;

/*

    Some references removed

*/

namespace TesseractChemistry
{
    public class TesseractChemistry : INonSharedRegionModule
    {
        /*

            Some code removed

        */

        private void CreateFromURL(string content, string format, string atomSize, string scale, string angularVelocity, string position)
        {
            try
            {
                SendMessageToAgents("Nouvelle molécule en cours de création...");

                Dictionary<UUID, SceneObjectGroup> atomList = new Dictionary<UUID, SceneObjectGroup>();
                Dictionary<UUID, SceneObjectGroup> bondList = new Dictionary<UUID, SceneObjectGroup>();
                Dictionary<UUID, SceneObjectGroup> linkedSOGList = new Dictionary<UUID, SceneObjectGroup>();

                int primCountInCurrentSOG = 0;
                SceneObjectGroup currentMoleculeSOG = null;

                // Parsing inworld position
                Vector3 inworldPosition = Vector3.Parse(position);

                // Getting content VIA WCF service
                LogInfo("Starting WCF Transaction...");

                OpenBabelServer.Service.Molecule molecule;
                NetTcpBinding netTcpBinding = new NetTcpBinding();
                netTcpBinding.MaxReceivedMessageSize = 104857600;

                using (ChannelFactory<IService> channel = new ChannelFactory<IService>(netTcpBinding, "net.tcp://localhost:7777"))
                {
                    LogInfo("Creating WCF channel...");
                    OpenBabelServer.Service.IService service;
                    service = channel.CreateChannel();

                    LogInfo("Querying WCF service for GetAtomsAndBondsFromURL...");
                    molecule = service.GetAtomsAndBondsFromURL(format, content);

                    LogInfo("Received response from WCF service and closing channel...");
                    (service as ICommunicationObject).Close();
                }

                // Molecule is now loaded : let's create it inworld !
                LogInfo("Rezzing molecule inworld...");

                PrimitiveBaseShape primBaseShape = PrimitiveBaseShape.CreateSphere();
                primBaseShape.Textures = new Primitive.TextureEntry(m_AtomTextureUUID);

                PrimitiveBaseShape bondBaseShape = PrimitiveBaseShape.CreateBox();
                bondBaseShape.ProfileShape = ProfileShape.Circle;
                bondBaseShape.Textures = new Primitive.TextureEntry(m_AtomTextureUUID);

                LogInfo("    Rezzing atoms...");
                SendMessageToAgents("Création des atomes...");
                foreach (Atom atom in molecule.Atoms)
                {
                    Vector3 atomPosition = new Vector3((float)atom.X, (float)atom.Y, (float)atom.Z);

                    PrimitiveBaseShape primShape = primBaseShape.Copy();
                    primShape.SetScale(float.Parse(atomSize));

                    SceneObjectGroup newPrim = m_scene.AddNewPrim(m_scene.RegionInfo.EstateSettings.EstateOwner, m_scene.RegionInfo.EstateSettings.EstateOwner, atomPosition + inworldPosition, new Quaternion(0, 0, 0), primShape);

                    if (atom.ElementType == "C")
                        newPrim.RootPart.SetFaceColorAlpha(SceneObjectPart.ALL_SIDES, new Vector3(0, 0, 0), (double)1);
                    if (atom.ElementType == "H")
                        newPrim.RootPart.SetFaceColorAlpha(SceneObjectPart.ALL_SIDES, new Vector3(1, 1, 1), (double)1);
                    if (atom.ElementType == "N")
                        newPrim.RootPart.SetFaceColorAlpha(SceneObjectPart.ALL_SIDES, new Vector3(0, 0, 1), (double)1);
                    if (atom.ElementType == "O")
                        newPrim.RootPart.SetFaceColorAlpha(SceneObjectPart.ALL_SIDES, new Vector3(1, 0, 0), (double)1);
                    if (atom.ElementType == "P")
                        newPrim.RootPart.SetFaceColorAlpha(SceneObjectPart.ALL_SIDES, new Vector3(1, 0.5f, 0), (double)1);
                    if (atom.ElementType == "S")
                        newPrim.RootPart.SetFaceColorAlpha(SceneObjectPart.ALL_SIDES, new Vector3(1, 1, 0.5f), (double)1);

                    newPrim.RootPart.Name = "Created with TesseractChemistry";
                    newPrim.RootPart.Description = "(c) 2013 Olivier BATTINI";
                    newPrim.Update();
                    atomList.Add(newPrim.UUID, newPrim);
                }

                LogInfo("    Rezzing bonds...");
                SendMessageToAgents("Création des liaisons covalentes...");
                foreach (Bond bond in molecule.Bonds)
                {
                    PrimitiveBaseShape bondShape = bondBaseShape.Copy();

                    Vector3 beginAtomPosition = new Vector3((float)bond.beginX, (float)bond.beginY, (float)bond.beginZ);
                    Vector3 endAtomPosition = new Vector3((float)bond.endX, (float)bond.endY, (float)bond.endZ);
                    float bondHeight = (float)Vector3.Distance(beginAtomPosition, endAtomPosition);
                    float bondRadius = 0.2f;
                    Vector3 bondPosition = beginAtomPosition + ((endAtomPosition - beginAtomPosition) / 2);
                    // Quaternion bondRotation = Vector3.RotationBetween(endAtomPosition - beginAtomPosition, new Vector3(0, 0, 1));
                    Vector3 bondNormalVector = endAtomPosition - beginAtomPosition;
                    bondNormalVector.Normalize();
                    Quaternion bondRotation = Quaternion.CreateFromEulers((float)Math.Asin(bondNormalVector.X), (float)Math.Asin(bondNormalVector.Y), (float)Math.Asin(bondNormalVector.Z));

                    // Work out the normalised vector from the source to the target
                    Vector3 delta = (endAtomPosition - bondPosition);
                    delta.Normalize();
                    Vector3 angle = new Vector3(0, 0, 0);

                    // Calculate the yaw
                    // subtracting PI_BY_TWO is required to compensate for the odd SL co-ordinate system
                    angle.X = (float)(Math.Atan2(delta.Z, delta.Y) - (Math.PI / 2));

                    // Calculate pitch
                    angle.Y = (float)(Math.Atan2(delta.X, Math.Sqrt((delta.Y * delta.Y) + (delta.Z * delta.Z))));

                    // we need to convert from a vector describing
                    // the angles of rotation in radians into rotation value

                    double x, y, z, s;

                    double c1 = Math.Cos(angle.X * 0.5);
                    double c2 = Math.Cos(angle.Y * 0.5);
                    double c3 = Math.Cos(angle.Z * 0.5);
                    double s1 = Math.Sin(angle.X * 0.5);
                    double s2 = Math.Sin(angle.Y * 0.5);
                    double s3 = Math.Sin(angle.Z * 0.5);

                    x = s1 * c2 * c3 + c1 * s2 * s3;
                    y = c1 * s2 * c3 - s1 * c2 * s3;
                    z = s1 * s2 * c3 + c1 * c2 * s3;
                    s = c1 * c2 * c3 - s1 * s2 * s3;

                    Quaternion rotation = new Quaternion((float)x, (float)y, (float)z, (float)s);

                    SceneObjectGroup newBond = m_scene.AddNewPrim(m_scene.RegionInfo.EstateSettings.EstateOwner, m_scene.RegionInfo.EstateSettings.EstateOwner, bondPosition + inworldPosition, new Quaternion(0, 0, 0), bondShape);
                    newBond.RootPart.RotationOffset = rotation;
                    newBond.RootPart.Resize(new Vector3(bondRadius, bondRadius, bondHeight));
                    newBond.RootPart.Name = "Created with TesseractChemistry";
                    newBond.RootPart.Description = "(c) 2013 Olivier BATTINI";
                    newBond.Update();

                    bondList.Add(newBond.UUID, newBond);
                }
                
                /*

                    Some code removed

                */
            }
            catch (Exception e)
            {
                m_log.Error(e.Message.ToString());
                m_log.Error(e.StackTrace.ToString());
            }
        }
    }
}
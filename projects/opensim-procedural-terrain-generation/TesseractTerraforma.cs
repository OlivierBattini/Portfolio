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
using System.Linq;
using System.Text;
using System.Drawing;
using System.Threading;
using System.Globalization;
using System.Reflection;

using Nini.Config;
using log4net;
using Mono.Addins;

/*

     Some references removed

*/

namespace TesseractTerraforma
{
    public class TesseractTerraforma : ISharedRegionModule, ICommandableModule
    {
        /*

             Some code removed

        */
        private void LoadConfiguration()
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
            try
            {
                if (m_ConfigSource.Configs["TesseractTerraforma"].GetBoolean("Enabled", false))
                {
                    IConfig config = m_ConfigSource.Configs["TesseractTerraforma"];

                    m_Seed = config.GetInt("Seed");
                    m_TerrainMinAltitude = config.GetInt("TerrainMinAltitude");
                    m_TerrainMaxAltitude = config.GetInt("TerrainMaxAltitude");
                    m_TerrainOffsetAltitude = config.GetInt("TerrainOffsetAltitude");
                    m_VegetationMinAltitude = config.GetFloat("VegetationMinAltitude");
                    m_VegetationMaxAltitude = config.GetFloat("VegetationMaxAltitude");
                    m_TreeProbability = config.GetFloat("TreeProbability");
                    m_GrassProbability = config.GetFloat("GrassProbability");

                    foreach (string treeType in config.GetString("TreeTypes").Split(','))
                        m_TreeTypes.Add(GetTreeTypeFromString(treeType));

                    foreach (string grassType in config.GetString("GrassTypes").Split(','))
                        m_GrassTypes.Add(GetGrassTypeFromString(grassType));
                }
            }
            catch (Exception ex)
            {

            }
        }

        private Tree GetTreeTypeFromString(string treeType)
        {
            switch (treeType)
            {
                case "BeachGrass1":
                    return Tree.BeachGrass1;

                case "Cypress1":
                    return Tree.Cypress1;

                case "Cypress2":
                    return Tree.Cypress2;

                case "Dogwood":
                    return Tree.Dogwood;

                case "Eelgrass":
                    return Tree.Eelgrass;

                case "Eucalyptus":
                    return Tree.Eucalyptus;

                case "Fern":
                    return Tree.Fern;

                case "Kelp1":
                    return Tree.Kelp1;

                case "Kelp2":
                    return Tree.Kelp2;

                case "Oak":
                    return Tree.Oak;

                case "Palm1":
                    return Tree.Palm1;

                case "Palm2":
                    return Tree.Palm2;

                case "Pine1":
                    return Tree.Pine1;

                case "Pine2":
                    return Tree.Pine2;

                case "Plumeria":
                    return Tree.Plumeria;

                case "SeaSword":
                    return Tree.SeaSword;

                case "TropicalBush1":
                    return Tree.TropicalBush1;

                case "TropicalBush2":
                    return Tree.TropicalBush2;

                case "WinterAspen":
                    return Tree.WinterAspen;

                case "WinterPine1":
                    return Tree.WinterPine1;

                case "WinterPine2":
                    return Tree.WinterPine2;

                default:
                    return Tree.Cypress2;
            }
        }

        private Grass GetGrassTypeFromString(string grassType)
        {
            switch (grassType)
            {
                case "Grass0":
                    return Grass.Grass0;

                case "Grass1":
                    return Grass.Grass1;

                case "Grass2":
                    return Grass.Grass2;

                case "Grass3":
                    return Grass.Grass3;

                case "Grass4":
                    return Grass.Grass4;

                case "Undergrowth1":
                    return Grass.Undergrowth1;

                default:
                    return Grass.Grass2;
            }
        }

        private void HandleTerraformaReset()
        {
            foreach (Scene scene in m_Scenes)
            {
                Thread th = new Thread(new ParameterizedThreadStart(Generate));
                th.Start(scene);
            }
        }

        private void HandleTerraformaRandomize()
        {
            m_Seed = new Random(m_Seed).Next(1000000);
            HandleTerraformaReset();
        }

        private void Generate(object scene)
        {
            Scene scn = (Scene)scene;

            m_log.Warn("[TesseractTerraforma] Deleting scene objects for " + scn.RegionInfo.RegionName);
            scn.DeleteAllSceneObjects();

            m_log.Warn("[TesseractTerraforma] Generating terrain for " + scn.RegionInfo.RegionName);
            int x = (int)scn.RegionInfo.RegionLocX;
            int y = (int)scn.RegionInfo.RegionLocY;
            string filename = String.Format("region_{0}_{1}.png", x, y);

            TesseractTerraformaRegion tf = new TesseractTerraformaRegion(x, y, m_Seed, m_TerrainMinAltitude, m_TerrainMaxAltitude, m_TerrainOffsetAltitude);
            tf.SaveToPng(filename);

            ITerrainModule tm = scn.RequestModuleInterface<ITerrainModule>();
            tm.LoadFromFile(filename);

            tf.GenerateVegetation(scn, m_VegetationMinAltitude, m_VegetationMaxAltitude, m_TreeProbability, m_GrassProbability, m_TreeTypes, m_Seed);
        }
    }


    class TesseractTerraformaRegion
    {
        private int[] _Data;
        private Bitmap _Map = new Bitmap(256, 256);

        public TesseractTerraformaRegion(int regionX, int regionY, int seed, int terrainMinAltitude, int terrainMaxAltitude, int terrainOffsetAltitude)
        {
            _Data = new int[256 * 256];
            PerlinNoise pn = new PerlinNoise(seed);
            int x, y;

            for (x = 0; x < 256; x++)
            {
                for (y = 0; y < 256; y++)
                {
                    double noise = (double)pn.Noise((float)(regionX * 256 + x) / 300, (float)(regionY * 256 + y) / 300);
                    int h = Math.Max(terrainMinAltitude, (int)Math.Floor((noise + 1) * terrainMaxAltitude) + terrainOffsetAltitude);

                    noise = (double)pn.Noise((float)(regionX * 256 + x) / 20, (float)(regionY * 256 + y) / 20);
                    h += (int)Math.Floor((noise + 1) / 2 * 10);

                    h = Math.Max(0, h);
                    h = Math.Min(255, h);

                    _Data[x + y * 256] = h;

                    Color c = Color.FromArgb(255, h, h, h);
                    _Map.SetPixel(x, 255 - y, c);
                }
            }
        }

        public void SaveToPng(string fileName)
        {
            _Map.Save(fileName);
        }

        public void GenerateVegetation(Scene scene, float minAltitude, float maxAltitude, float treeProbability, float grassProbability, List<Tree> treeTypes, int seed)
        {
            Console.WriteLine("DELETING SCENE OBJECTS...");
            scene.DeleteAllSceneObjects();
            //scene.Backup(true);

            int x, y;
            Random r = new Random(seed);

            for (x = 0; x < 256; x += r.Next(20))
            {
                for (y = 0; y < 256; y += r.Next(20))
                {
                    float z = (float)scene.Heightmap[x, y];
                    double p = r.NextDouble();
                    float s = (float)r.NextDouble();

                    if (p > treeProbability && z > minAltitude && z < maxAltitude)
                    {
                        //Console.WriteLine(String.Format("PLANTING A TREE @ {0},{1}", x, y));
                        Vector3 scale = new Vector3(s, s, s);

                        PrimitiveBaseShape treeShape = new PrimitiveBaseShape();
                        treeShape.PathCurve = 16;
                        treeShape.PathEnd = 49900;
                        treeShape.PCode = (byte)PCode.Tree;
                        treeShape.Scale = scale;
                        treeShape.State = (byte)treeTypes[r.Next(treeTypes.Count)];

                        SceneObjectGroup tree = scene.AddNewPrim(UUID.Random(), UUID.Zero, new Vector3((float)x, (float)y, (float)z),
                            Quaternion.Identity, treeShape);

                        tree.OwnerID = scene.RegionInfo.EstateSettings.EstateOwner;
                        tree.SendGroupFullUpdate();
                    }
                }
            }

            for (x = 0; x < 256; x += r.Next(10))
            {
                for (y = 0; y < 256; y += r.Next(10))
                {
                    float z = (float)scene.Heightmap[x, y];
                    double p = r.NextDouble();
                    float s = (float)(p) * 5;

                    if (p > grassProbability && z > minAltitude && z < maxAltitude)
                    {
                        //Console.WriteLine(String.Format("PLANTING GRASS @ {0},{1}", x, y));
                        Vector3 scale = new Vector3(s, s, s);
                        Grass grassType = (Grass)r.Next(5);

                        PrimitiveBaseShape grassShape = new PrimitiveBaseShape();
                        grassShape.PathCurve = 16;
                        grassShape.PathEnd = 49900;
                        grassShape.PCode = (byte)PCode.Grass;
                        grassShape.Scale = scale;
                        grassShape.State = (byte)grassType;

                        SceneObjectGroup tree = scene.AddNewPrim(UUID.Random(), UUID.Zero, new Vector3((float)x, (float)y, (float)z),
                            Quaternion.Identity, grassShape);

                        tree.OwnerID = scene.RegionInfo.EstateSettings.EstateOwner;
                        tree.SendGroupFullUpdate();
                    }
                }
            }
        }
    }

    public sealed class PerlinNoise
    {
        #region Constants

        private int RANDOM_SIZE = 256;

        #endregion

        #region Fields

        private readonly int[] values;

        #endregion

        public PerlinNoise(int seed)
        {
            this.values = new int[RANDOM_SIZE * 2];
            Random generator = new Random(seed);
            byte[] source = new byte[RANDOM_SIZE];
            generator.NextBytes(source);

            for (int i = 0; i < RANDOM_SIZE; i++)
                this.values[i + RANDOM_SIZE] = this.values[i] = source[i];
        }

        public float Noise(float x)
        {
            int X = (int)Math.Floor(x) & 255;
            x -= (float)Math.Floor(x);
            float u = Fade(x);

            return Lerp(u, Grad(this.values[X], x), Grad(this.values[X + 1], x - 1));
        }

        public float Noise(float x, float y)
        {
            int X = (int)Math.Floor(x) & 255;
            int Y = (int)Math.Floor(y) & 255;
            x -= (float)Math.Floor(x);
            y -= (float)Math.Floor(y);
            float u = Fade(x);
            float v = Fade(y);
            int A = this.values[X] + Y;
            int B = this.values[X + 1] + Y;

            return Lerp(v, Lerp(u, Grad(this.values[A], x, y),
                                   Grad(this.values[B], x - 1, y)),
                           Lerp(u, Grad(this.values[A + 1], x, y - 1),
                                   Grad(this.values[B + 1], x - 1, y - 1)));
        }

        public float Noise(float x, float y, float z)
        {
            int X = (int)Math.Floor(x) & 255;
            int Y = (int)Math.Floor(y) & 255;
            int Z = (int)Math.Floor(z) & 255;
            x -= (float)Math.Floor(x);
            y -= (float)Math.Floor(y);
            z -= (float)Math.Floor(z);
            float u = Fade(x);
            float v = Fade(y);
            float w = Fade(z);
            int A = this.values[X] + Y;
            int AA = this.values[A] + Z;
            int AB = this.values[A + 1] + Z;
            int B = this.values[X + 1] + Y;
            int BA = this.values[B] + Z;
            int BB = this.values[B + 1] + Z;

            return Lerp(w, Lerp(v, Lerp(u, Grad(this.values[AA], x, y, z),
                                           Grad(this.values[BA], x - 1, y, z)),
                                   Lerp(u, Grad(this.values[AB], x, y - 1, z),
                                           Grad(this.values[BB], x - 1, y - 1, z))),
                           Lerp(v, Lerp(u, Grad(this.values[AA + 1], x, y, z - 1),
                                           Grad(this.values[BA + 1], x - 1, y, z - 1)),
                                   Lerp(u, Grad(this.values[AB + 1], x, y - 1, z - 1),
                                           Grad(this.values[BB + 1], x - 1, y - 1, z - 1))));
        }

        /*

             Some code removed

        */
    }
}
﻿using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using BitMiracle.LibTiff.Classic;

namespace Cognitics.OpenFlight
{
    public enum RecordType : ushort
    {
        Invalid = 0,
        Header = 1,
        Group = 2,
        Object = 4,
        Face = 5,
        PushLevel = 10,
        PopLevel = 11,
        DegreeOfFreedom = 14,
        PushSubFace = 19,
        PopSubFace = 20,
        PushExtension = 21,
        PopExtension = 22,
        Continuation = 23,
        Comment = 31,
        ColorPalette = 32,
        LongId = 33,
        Matrix = 49,
        Vector = 50,
        MultiTexture = 52,
        UvList = 53,
        BinarySeparatingPlane = 55,
        Replicate = 60,
        InstanceReference = 61,
        InstanceDefinition = 62,
        ExternalReference = 63,
        TexturePalette = 64,
        VertexPalette = 67,
        VertexWithColor = 68,
        VertexWithColorNormal = 69,
        VertexWithColorNormalUv = 70,
        VertexWithColorUv = 71,
        VertexList = 72,
        LevelOfDetail = 73,
        BoundingBox = 74,
        RotateAboutEdge = 76,
        Translate = 78,
        Scale = 79,
        RotateAboutPoint = 80,
        RotateScaleToPoint = 81,
        Put = 82,
        EyePointTrackPlanePalette = 83,
        Mesh = 84,
        LocalVertexPool = 85,
        MeshPrimitive = 86,
        RoadSegment = 87,
        RoadZone = 88,
        MorphVertexList = 89,
        LinkagePalette = 90,
        Sound = 91,
        RoadPath = 92,
        SoundPalette = 93,
        GeneralMatrix = 94,
        Text = 95,
        Switch = 96,
        LineStylePalette = 97,
        ClipRegion = 98,
        Extension = 100,
        LightSource = 101,
        LightSourcePalette = 102,
        BoundingSphere = 105,
        BoundingCylinder = 106,
        BoundingConvexHull = 107,
        BoundingVolumeCenter = 108,
        BoundingVolumeOrientation = 109,
        LightPoint = 111,
        TextureMappingPalette = 112,
        MaterialPalette = 113,
        NameTable = 114,
        Cat = 115,
        CatData = 116,
        BoundingHistogram = 119,
        PushAttribute = 122,
        PopAttribute = 123,
        Curve = 126,
        RoadConstruction = 127,
        LightPointAppearancePalette = 128,
        LightPointAnimationPalette = 129,
        IndexedLightPoint = 130,
        LightPointSystem = 131,
        IndexedString = 132,
        ShaderPalette = 133,
        //Extended*
        ExtendedMaterialHeader = 135,
        ExtendedMaterialAmbient = 136,
        ExtendedMaterialDiffuse = 137,
        ExtendedMaterialSpecular = 138,
        ExtendedMaterialEmissive = 139,
        ExtendedMaterialAlpha = 140,
        ExtendedMaterialLightMap = 141,
        ExtendedMaterialNormalMap = 142,
        ExtendedMaterialBumpMap = 143,
        ExtendedMaterialShadowMap = 145,
        ExtendedMaterialReflectionMap = 147,
    }

    public class FltReader
    {
        public static string GetString(byte[] bytes)
        {
            int offset = bytes.Length;
            for (int i = 0; i < bytes.Length; i++)
            {
                if (bytes[i] == 0)
                {
                    // Found the terminator
                    offset = i;
                    break;
                }
            }

            return Encoding.ASCII.GetString(bytes, 0, offset);
        }

        public long Position = 0;
        public RecordType Opcode = RecordType.Invalid;
        public ushort RecordLength = 0;
        public bool Postpone = false;

        public Mesh CurrentMeshRecord = null;

        public BinaryParser BinaryParser;

        public FltReader(BinaryParser input) { BinaryParser = input; }

        // Move the file pointer to the start of the next record
        public bool Advance()
        {
            if (Postpone)
                Postpone = false;
            else
                Position += RecordLength;

            if (BinaryParser.Bytes.Length - Position < 4)
                return false;
            BinaryParser.Position = (int)Position;
            return true;
        }

        public Record Process(Record parent, RecordType recordType, FltReader reader)
        {
            NextOpcode();
            RecordLength = BinaryParser.UInt16BE();

            return CreateRecord(parent, recordType, reader);
        }

        public RecordType NextOpcode(bool advance = true)
        {
            Position = BinaryParser.Position;
            Opcode = (RecordType)BinaryParser.UInt16BE();
            if (!advance)
                BinaryParser.Position = (int)Position;
            return Opcode;
        }

        public Record CreateRecord(Record parent, RecordType recordType, FltReader reader)
        {
            //Console.WriteLine("Record.Parse: recordType: " + recordType.ToString());

            Record baseData = null;
            if (recordType == RecordType.Header)
            {
                var data = new Header(parent, recordType, reader);
                data.Parse();
                data.Root.Header = data;
                baseData = data;
            }
            else if (recordType == RecordType.Comment)
            {
                var data = new Comment(parent, recordType, reader);
                data.Parse();
                baseData = data;
            }
            //else if (recordType == RecordType.BoundingSphere)
            //{
            //    var data = new BoundingSphere(parent, recordType, reader);
            //    data.Parse();
            //    baseData = data;
            //}
            //else if (recordType == RecordType.BoundingBox)
            //{
            //    var data = new BoundingBox(parent, recordType, reader);
            //    data.Parse();
            //    baseData = data;
            //}
            //else if (recordType == RecordType.BoundingVolumeCenter)
            //{
            //    var data = new BoundingVolumeCenter(parent, recordType, reader);
            //    data.Parse();
            //    baseData = data;
            //}
            //else if (recordType == RecordType.BoundingVolumeOrientation)
            //{
            //    var data = new BoundingVolumeOrientation(parent, recordType, reader);
            //    data.Parse();
            //    baseData = data;
            //}
            //else if (recordType == RecordType.ColorPalette)
            //{
            //    var data = new ColorPalette(parent, recordType, reader);
            //    if (data.Root.ColorPalette != null)
            //        Console.WriteLine("ERROR: more than one color palette! not handled");
            //    data.Root.ColorPalette = data;
            //    data.Parse();
            //    baseData = data;
            //}
            else if (recordType == RecordType.MaterialPalette)
            {
                var data = new MaterialPalette(parent, recordType, reader);
                data.Root.MaterialPalettes.Add(data);
                data.Parse();
                baseData = data;
            }
            else if (recordType == RecordType.TexturePalette)
            {
                var data = new TexturePalette(parent, recordType, reader);
                data.Root.TexturePalettes.Add(data);
                data.Parse();
                baseData = data;
            }
            else if (recordType == RecordType.VertexPalette)
            {
                var data = new VertexPalette(parent, recordType, reader);
                if (data.Root.VertexPalette != null)
                    Console.WriteLine("ERROR: more than one vertex palette! not handled");
                data.Root.VertexPalette = data;
                data.Parse();
                baseData = data;
            }
            else if (recordType == RecordType.PushLevel)
            {
                var data = new PushLevel(parent, recordType, reader);
                parent.Root.PushStack.Push(data);
                data.Parse();
                baseData = data;
            }
            else if (recordType == RecordType.PopLevel)
            {
                var data = new PopLevel(parent, recordType, reader);
                data.Parse();
                baseData = data;
            }
            //else if (recordType == RecordType.Matrix)
            //{
            //    var data = new Matrix(parent, recordType, reader);
            //    data.Parse();
            //    baseData = data;
            //}
            //else if (recordType == RecordType.GeneralMatrix)
            //{
            //    var data = new GeneralMatrix(parent, recordType, reader);
            //    data.Parse();
            //    baseData = data;
            //}
            else if (recordType == RecordType.Group)
            {
                var data = new Group(parent, recordType, reader);
                data.Parse();
                baseData = data;
            }
            else if (recordType == RecordType.Mesh)
            {
                var data = new Mesh(parent, recordType, reader);
                data.Parse();
                baseData = data;
            }
            else if (recordType == RecordType.LocalVertexPool)
            {
                var data = new LocalVertexPool(parent, recordType, reader);
                data.Parse();
                baseData = data;
            }
            else if (recordType == RecordType.MeshPrimitive)
            {
                var data = new MeshPrimitive(parent, recordType, reader);
                data.Parse();
                baseData = data;
            }
            else if (recordType == RecordType.Face)
            {
                var data = new Face(parent, recordType, reader);
                data.Parse();
                baseData = data;
            }
            else if (recordType == RecordType.LevelOfDetail)
            {
                var data = new LevelOfDetail(parent, recordType, reader);
                data.Parse();
                baseData = data;
            }
            else if (recordType == RecordType.VertexList)
            {
                var data = new VertexList(parent, recordType, reader);
                data.Parse();
                baseData = data;
            }
            else if (recordType == RecordType.VertexWithColor)
            {
                var data = new VertexWithColor(parent, recordType, reader);
                data.Parse();
                baseData = data;
            }
            else if (recordType == RecordType.VertexWithColorNormal)
            {
                var data = new VertexWithColorNormal(parent, recordType, reader);
                data.Parse();
                baseData = data;
            }
            else if (recordType == RecordType.VertexWithColorNormalUv)
            {
                var data = new VertexWithColorNormalUV(parent, recordType, reader);
                data.Parse();
                baseData = data;
            }
            else if (recordType == RecordType.VertexWithColorUv)
            {
                var data = new VertexWithColorUV(parent, recordType, reader);
                data.Parse();
                baseData = data;
            }
            else if (recordType == RecordType.ExternalReference)
            {
                var data = new ExternalReference(parent, recordType, reader);
                data.Parse();
                baseData = data;
            }
            //else if (recordType == RecordType.ExtendedMaterialHeader)
            //{
            //    var data = new ExtendedMaterialHeader(parent, recordType, reader);
            //    data.Parse();
            //    baseData = data;
            //}
            //else if (recordType == RecordType.MultiTexture)
            //{
            //    var data = new MultiTexture(parent, recordType, reader);
            //    data.Parse();
            //    baseData = data;
            //}
            //else if (recordType == RecordType.UvList)
            //{
            //    var data = new UvList(parent, recordType, reader);
            //    data.Parse();
            //    baseData = data;
            //}
            //else
            //{
            //    Console.WriteLine("Record.Parse: recordType: " + recordType.ToString());
            //}

            return baseData;
        }
    }

    #region Geometry

    public class Material
    {
        public MaterialPalette materialPalette = null;
        public TexturePalette mainTexturePalette = null;
        public TexturePalette detailTexturePalette = null;

        public Material(MaterialPalette _materialPalette, TexturePalette _mainTexturePalette, TexturePalette _detailTexturePalette)
        {
            materialPalette = _materialPalette;
            mainTexturePalette = _mainTexturePalette;
            detailTexturePalette = _detailTexturePalette;
        }

        public bool Equals(MaterialPalette otherMaterialPalette, TexturePalette otherMainTexturePalette, TexturePalette otherDetailTexturePalette)//, ushort otherTransparency, Face.LightMode otherLightMode)
        {
            return Equals(materialPalette, otherMaterialPalette) &&
                Equals(mainTexturePalette, otherMainTexturePalette) &&
                Equals(detailTexturePalette, otherDetailTexturePalette);
        }
    }

    public class Submesh
    {
        public Material material = null;
        // TODO: attempt standard array instead of list
        public List<int> triangles = new List<int>();
        public List<int> backfaceTriangles = new List<int>();

        public Submesh(Material _material)
        {
            material = _material;
        }
    }

    public struct Vector
    {
        public float x, y, z;
        public Vector(float _x, float _y, float _z) { x = _x; y = _y; z = _z; }
    }

    public struct UV
    {
        public float u, v;
        public UV(float _u, float _v) { u = _u; v = _v; }
    }

    #endregion

    #region Records

    public abstract class Record
    {
        public FltReader Reader = null;
        public FltDatabase Root = null;
        public Record Parent = null;
        public List<Record> Children = new List<Record>();
        public RecordType RecordType = RecordType.Invalid;

        public Record(Record parent, RecordType recordType, FltReader reader)
        {
            Reader = reader;
            Root = parent != null ? parent.Root : null;
            Parent = parent;
            if (Parent != null)
                Parent.Children.Add(this);
            RecordType = recordType;
        }

        public virtual void Parse() // base Parse is called by any record that has a potential hierarchy, including the flt database itself
        {
            while (Reader.Advance()) // make sure we're at the next record's start
            {
                // Peek
                var nextRecordType = Reader.NextOpcode(false);

                // Determine whether we need to return control to parent because a given record isn't meant to process the next record as a child (per the spec)
                if (RecordType == RecordType.VertexPalette)
                {
                    if (nextRecordType != RecordType.VertexWithColor &&
                        nextRecordType != RecordType.VertexWithColorNormal &&
                        nextRecordType != RecordType.VertexWithColorNormalUv &&
                        nextRecordType != RecordType.VertexWithColorUv)
                    {
                        Reader.Postpone = true;
                        break;
                    }
                }
                else if (RecordType == RecordType.Object || 
                         RecordType == RecordType.Group || 
                         RecordType == RecordType.LevelOfDetail)
                         //RecordType == RecordType.DegreeOfFreedom || 
                         //RecordType == RecordType.Switch || 
                         //RecordType == RecordType.Invalid)
                {
                    if (!Root.IdRecords.Contains(this as IdRecord)) // TODO: fix bug causing dupes in the record list so this check can go away
                        Root.IdRecords.Add(this as IdRecord);
                    if (nextRecordType != RecordType.PushLevel &&
                        nextRecordType != RecordType.LongId &&
                        nextRecordType != RecordType.Comment &&
                        nextRecordType != RecordType.Matrix)
                    {
                        Reader.Postpone = true;
                        break;
                    }
                }
                else if (RecordType == RecordType.Face)
                {
                    if (nextRecordType != RecordType.PushLevel &&
                        nextRecordType != RecordType.LongId &&
                        nextRecordType != RecordType.MultiTexture &&
                        nextRecordType != RecordType.Comment)
                    {
                        Reader.Postpone = true;
                        break;
                    }
                }
                else if (RecordType == RecordType.ExternalReference)
                {
                    if (nextRecordType != RecordType.Matrix)
                    {
                        Reader.Postpone = true;
                        break;
                    }
                }
                //else if (RecordType == RecordType.ExtendedMaterialHeader)
                //{
                //    if (nextRecordType != RecordType.ExtendedMaterialAmbient || 
                //        nextRecordType != RecordType.ExtendedMaterialDiffuse || 
                //        nextRecordType != RecordType.ExtendedMaterialSpecular || 
                //        nextRecordType != RecordType.ExtendedMaterialEmissive || 
                //        nextRecordType != RecordType.ExtendedMaterialAlpha || 
                //        nextRecordType != RecordType.ExtendedMaterialLightMap || 
                //        nextRecordType != RecordType.ExtendedMaterialNormalMap || 
                //        nextRecordType != RecordType.ExtendedMaterialBumpMap || 
                //        nextRecordType != RecordType.ExtendedMaterialShadowMap || 
                //        nextRecordType != RecordType.ExtendedMaterialReflectionMap)
                //    {
                //        Reader.Postpone = true;
                //        break;
                //    }
                //}

                // Calculate the parent
                Record parent = null;
                if (nextRecordType == RecordType.PopLevel)
                {
                    PushLevel push = null;
                    try
                    {
                        push = Root.PushStack.Pop();
                    }
                    catch
                    {
                        Console.WriteLine("ERROR: pop not paired with a prior push!");
                        break;
                    }
                    parent = push;
                }
                else
                {
                    parent = this;
                }

                var record = Reader.Process(parent, nextRecordType, Reader);
                if (record != null && record.RecordType == RecordType.PopLevel)
                {
                    if (RecordType != RecordType.PushLevel)
                        Console.WriteLine("ERROR: push/pop mismatch");
                    break;
                }
            }
        }

        public IdRecord FindParentIdRecord()
        {
            var parent = Parent;
            while (parent != null)
            {
                if (parent is IdRecord)
                    return parent as IdRecord;
                else
                    parent = parent.Parent;
            }

            return null;
        }

        public Face FindParentFace()
        {
            var parent = Parent;
            while (parent != null)
            {
                if (parent is Face)
                    return parent as Face;
                else
                    parent = parent.Parent;
            }

            return null;
        }
    }

    public abstract class IdRecord : Record
    {
        public string idStr = null;
        // TODO: attempt standard array instead of list
        public List<Submesh> Submeshes = null;

        protected const int fixedIdLength = 8;

        public IdRecord(Record parent, RecordType recordType, FltReader reader) : base(parent, recordType, reader)
        {
        }

        public void InitGeometry()
        {
            if (Submeshes == null)
                Submeshes = new List<Submesh>();
        }

        public bool HasGeometry()
        {
            return Submeshes != null && Submeshes.Count != 0;
        }

        public Submesh GetOrAddSubmesh(/*Face face, */Material material)
        {
            foreach (var existingSubmesh in Submeshes)
            {
                var subMeshMaterial = existingSubmesh.material;
                if (subMeshMaterial.Equals(material.materialPalette, material.mainTexturePalette, material.detailTexturePalette))//, face.transparency, face.lightMode))
                    return existingSubmesh;
            }

            var newSubmesh = new Submesh(material);
            Submeshes.Add(newSubmesh);

            return newSubmesh;
        }
    }

    public class FltDatabase : IdRecord // NOTE: not a true IdRecord, in that it has no idStr. this is the ultimate fallback for generating a geometry
    {
        public Header Header = null;
        public ColorPalette ColorPalette = null;
        public List<MaterialPalette> MaterialPalettes = new List<MaterialPalette>();
        public List<TexturePalette> TexturePalettes = new List<TexturePalette>();
        public VertexPalette VertexPalette = null;
        public Stack<PushLevel> PushStack = new Stack<PushLevel>();
        public string Path = null;
        public List<IdRecord> geometryRecords = null;
        public List<IdRecord> IdRecords = new List<IdRecord>();
        public List<Material> Materials = new List<Material>();
        public List<FltDatabase> ExternalDBs = new List<FltDatabase>();

        public FltDatabase(Record parent, RecordType recordType, FltReader reader, string path) : base(parent, recordType, reader)
        {
            Root = this;
            Reader = reader;
            RecordType = recordType;
            Path = path;
            InitGeometry(); // TODO: don't always do this, usually an idRecord has the geometry we care about
        }

        public override void Parse()
        {
            base.Parse();
            geometryRecords = SelectGeometryRecords();
        }

        public Material GetOrAddMaterial(Face face)
        {
            return GetOrAddMaterial(face.materialIndex, face.texturePatternIndex, face.detailTexturePatternIndex);
        }

        public Material GetOrAddMaterial(short materialIndex, short texturePatternIndex, short detailTexturePatternIndex)
        {
            var materialPalette = FindMaterialPalette(materialIndex);
            var mainTexturePalette = FindMainTexturePalette(texturePatternIndex);
            var detailTexturePalette = FindDetailTexturePalette(detailTexturePatternIndex);

            foreach (var existingMaterial in Materials)
            {
                if (existingMaterial.Equals(materialPalette, mainTexturePalette, detailTexturePalette))//, face.transparency, face.lightMode))
                    return existingMaterial;
            }

            var newMaterial = new Material(materialPalette, mainTexturePalette, detailTexturePalette);
            Materials.Add(newMaterial);

            return newMaterial;
        }

        private MaterialPalette FindMaterialPalette(short index)
        {
            if (index != -1)
            {
                foreach (var palette in MaterialPalettes)
                {
                    if (palette.materialIndex == index)
                        return palette;
                }
            }

            return null;
        }

        private TexturePalette FindMainTexturePalette(short index)
        {
            if (index != -1)
            {
                foreach (var palette in TexturePalettes)
                {
                    if (palette.texturePatternIndex == index)
                        return palette;
                }
            }

            return null;
        }

        private TexturePalette FindDetailTexturePalette(short index)
        {
            if (index != -1)
            {
                foreach (var palette in TexturePalettes)
                {
                    if (palette.texturePatternIndex == index)
                        return palette;
                }
            }

            return null;
        }

        private List<IdRecord> SelectGeometryRecords()
        {
            var _geometryRecords = new List<IdRecord>();

            for (int i = IdRecords.Count - 1; i >= 0; --i)
            {
                var idRecord = IdRecords[i];
                if (idRecord.RecordType == RecordType.LevelOfDetail)
                {
                    if (idRecord.HasGeometry())
                    {
                        _geometryRecords.Add(idRecord);
                    }
                    else
                    {
                        for (int j = 0; j < IdRecords.Count; ++j)
                        {
                            var idRecord2 = IdRecords[j];
                            if (idRecord2.RecordType == RecordType.Group)
                            {
                                var parent = idRecord2.FindParentIdRecord();
                                if (parent != null && parent == idRecord && idRecord2.HasGeometry())
                                {
                                    _geometryRecords.Add(idRecord2);
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            //for (int i = ExternalDBs.Count - 1; i >= 0; --i)
            //{
            //    var externalDB = ExternalDBs[i];
            //    var externalGeometryRecords = externalDB.SelectGeometryRecords();
            //    _geometryRecords.AddRange(externalGeometryRecords);
            //}

            // fallback
            if (_geometryRecords.Count == 0)
            {
                for (int i = 0; i < IdRecords.Count; ++i)
                {
                    var idRecord = IdRecords[i];
                    if (idRecord.HasGeometry())
                        _geometryRecords.Add(idRecord);
                }
            }

            // last fallback
            if (_geometryRecords.Count == 0 && Root.HasGeometry())
                _geometryRecords.Add(Root);

            int geomCount = _geometryRecords.Count;

            return _geometryRecords;
        }
    }

    public class Header : IdRecord
    {
        static bool skipUnusedFields = true; // if we don't use it (yet), skip it

        public int formatRevisionLevel;
        public int editRevisionLevel;
        public string lastRevisionDateTime;
        public short nextGroupNodeId;
        public short nextLodNodeId;
        public short nextObjectNodeId;
        public short nextFaceNodeId;
        public short unitMultiplier;
        public VertexCoordinateUnits vertexCoordinateUnits;
        public byte texWhite;
        public Flags flags;
        public ProjectionType projectionType;
        public short nextDofNodeId;
        public short vertexStorageType;
        public DatabaseOrigin databaseOrigin;
        public double southwestX;
        public double southwestY;
        public double deltaX;
        public double deltaY;
        public short nextSoundNodeId;
        public short nextPathNodeId;
        public short nextClipNodeId;
        public short nextTextNodeId;
        public short nextBspNodeId;
        public short nextSwitchNodeId;
        public double southwestLatitude;
        public double southwestLongitude;
        public double northeastLatitude;
        public double northeastLongitude;
        public double originLatitude;
        public double originLongitude;
        public double lambertUpperLatitude;
        public double lambertLowerLongitude;
        public short nextLightSourceNodeId;
        public short nextLightPointNodeId;
        public short nextRoadNodeId;
        public short nextCatNodeId;
        public EarthEllipsoidModel earthEllipsoidModel;
        public short nextAdaptiveNodeId;
        public short nextCurveNodeId;
        public short utmZone;

        public enum VertexCoordinateUnits
        {
            Meters = 0,
            Kilometers = 1,
            Feet = 4,
            Inches = 5,
            NauticalMiles = 8,
        }

        public enum Flags
        {
            SaveVertexNormals = 0,
            PackedColorMode = 1,
            CadViewMode = 2,
        }

        public enum ProjectionType
        {
            FlatEarth = 0,
            Trapezoidal = 1,
            RoundEarth = 2,
            Lambert = 3,
            Utm = 4,
            Geodetic = 5,
            Geocentric = 6,
        }

        public enum DatabaseOrigin
        {
            OpenFlight = 100,
            DIG_1_OR_2 = 200,
            ES_CT5A_OR_CT6 = 300,
            PSP_DIG = 400,
            GE_CIV_OR_CV_OR_PT2000 = 600,
            ES_GDF = 700,
        }

        public enum EarthEllipsoidModel
        {
            UserDefined = -1,
            WGS_1984 = 0,
            WGS_1972 = 1,
            Bessel = 2,
            Clarke_1866 = 3,
            NAD_1927 = 4,
        }

        public Header(Record parent, RecordType recordType, FltReader reader) : base(parent, recordType, reader) {}

        public override void Parse()
        {
            idStr = Reader.BinaryParser.String(fixedIdLength);
            formatRevisionLevel = Reader.BinaryParser.Int32BE();
            if (skipUnusedFields)
                return;

            editRevisionLevel = Reader.BinaryParser.Int32BE();
            lastRevisionDateTime = Reader.BinaryParser.String(32);
            nextGroupNodeId = Reader.BinaryParser.Int16BE();
            nextLodNodeId = Reader.BinaryParser.Int16BE();
            nextObjectNodeId = Reader.BinaryParser.Int16BE();
            nextFaceNodeId = Reader.BinaryParser.Int16BE();
            unitMultiplier = Reader.BinaryParser.Int16BE();
            vertexCoordinateUnits = (VertexCoordinateUnits)Reader.BinaryParser.Byte();
            texWhite = Reader.BinaryParser.Byte();
            flags = (Flags)Reader.BinaryParser.Int32BE();

            Reader.BinaryParser.Position += 6 * sizeof(int);

            projectionType = (ProjectionType)Reader.BinaryParser.Int32BE();

            Reader.BinaryParser.Position += 7 * sizeof(int);

            nextDofNodeId = Reader.BinaryParser.Int16BE();
            vertexStorageType = Reader.BinaryParser.Int16BE();
            databaseOrigin = (DatabaseOrigin)Reader.BinaryParser.Int32BE();
            southwestX = Reader.BinaryParser.DoubleBE();
            southwestY = Reader.BinaryParser.DoubleBE();
            deltaX = Reader.BinaryParser.DoubleBE();
            deltaY = Reader.BinaryParser.DoubleBE();
            nextSoundNodeId = Reader.BinaryParser.Int16BE();
            nextPathNodeId = Reader.BinaryParser.Int16BE();

            Reader.BinaryParser.Position += 2 * sizeof(int);

            nextClipNodeId = Reader.BinaryParser.Int16BE();
            nextTextNodeId = Reader.BinaryParser.Int16BE();
            nextBspNodeId = Reader.BinaryParser.Int16BE();
            nextSwitchNodeId = Reader.BinaryParser.Int16BE();

            Reader.BinaryParser.Position += 1 * sizeof(int);

            southwestLatitude = Reader.BinaryParser.DoubleBE();
            southwestLongitude = Reader.BinaryParser.DoubleBE();
            northeastLatitude = Reader.BinaryParser.DoubleBE();
            northeastLongitude = Reader.BinaryParser.DoubleBE();
            originLatitude = Reader.BinaryParser.DoubleBE();
            originLongitude = Reader.BinaryParser.DoubleBE();
            lambertUpperLatitude = Reader.BinaryParser.DoubleBE();
            lambertLowerLongitude = Reader.BinaryParser.DoubleBE();
            nextLightSourceNodeId = Reader.BinaryParser.Int16BE();
            nextLightPointNodeId = Reader.BinaryParser.Int16BE();
            nextRoadNodeId = Reader.BinaryParser.Int16BE();
            nextCatNodeId = Reader.BinaryParser.Int16BE();

            Reader.BinaryParser.Position += 4 * sizeof(short);

            earthEllipsoidModel = (EarthEllipsoidModel)Reader.BinaryParser.Int32BE();
            nextAdaptiveNodeId = Reader.BinaryParser.Int16BE();
            nextCurveNodeId = Reader.BinaryParser.Int16BE();
            if (formatRevisionLevel < 1580)
                return;

            utmZone = Reader.BinaryParser.Int16BE();

            //Reader.ReadBytes(6);

            //double deltaZ = Reader.BinaryParser.DoubleBE();
            //double radius = Reader.BinaryParser.DoubleBE();
            //ushort nextMeshNodeId = (ushort)Reader.BinaryParser.Int16BE();
            //ushort nextLightPointSystemId = (ushort)Reader.BinaryParser.Int16BE();

            //if (formatRevisionLevel < 1580)
            //    return;

            //Reader.ReadBytes(1 * sizeof(int));

            //double earthMajorAxis = Reader.BinaryParser.DoubleBE();
            //double earthMinorAxis = Reader.BinaryParser.DoubleBE();
        }
    }

    public class Comment : Record
    {
        public string commentStr = null;

        public Comment(Record parent, RecordType recordType, FltReader reader) : base(parent, recordType, reader) {}

        public override void Parse()
        {
            Reader.BinaryParser.Int16BE(); // length
            commentStr = Reader.BinaryParser.String(Reader.RecordLength - 4);
        }
    }

    public class BoundingSphere : Record
    {
        double radius = 0;

        public BoundingSphere(Record parent, RecordType recordType, FltReader reader) : base(parent, recordType, reader) {}

        public override void Parse()
        {
            Reader.BinaryParser.Int32BE();

            radius = Reader.BinaryParser.DoubleBE();
        }
    }

    public class BoundingBox : Record
    {
        double minX = 0;
        double minY = 0;
        double minZ = 0;
        double maxX = 0;
        double maxY = 0;
        double maxZ = 0;

        public BoundingBox(Record parent, RecordType recordType, FltReader reader) : base(parent, recordType, reader) {}

        public override void Parse()
        {
            Reader.BinaryParser.Int32BE();

            minX = Reader.BinaryParser.DoubleBE();
            minY = Reader.BinaryParser.DoubleBE();
            minZ = Reader.BinaryParser.DoubleBE();
            maxX = Reader.BinaryParser.DoubleBE();
            maxY = Reader.BinaryParser.DoubleBE();
            maxZ = Reader.BinaryParser.DoubleBE();
        }
    }

    public class BoundingVolumeCenter : Record
    {
        double x = 0;
        double y = 0;
        double z = 0;

        public BoundingVolumeCenter(Record parent, RecordType recordType, FltReader reader) : base(parent, recordType, reader) {}

        public override void Parse()
        {
            Reader.BinaryParser.Int32BE();

            x = Reader.BinaryParser.DoubleBE();
            y = Reader.BinaryParser.DoubleBE();
            z = Reader.BinaryParser.DoubleBE();
        }
    }

    public class BoundingVolumeOrientation : Record
    {
        double yaw = 0;
        double pitch = 0;
        double roll = 0;

        public BoundingVolumeOrientation(Record parent, RecordType recordType, FltReader reader) : base(parent, recordType, reader) {}

        public override void Parse()
        {
            Reader.BinaryParser.Int32BE();

            yaw = Reader.BinaryParser.DoubleBE();
            pitch = Reader.BinaryParser.DoubleBE();
            roll = Reader.BinaryParser.DoubleBE();
        }
    }

    public class ColorPalette : Record
    {
        public struct Color
        {
            public string name;
            public byte[] argb;
        }

        public Color[] colors = new Color[1024];

        public ColorPalette(Record parent, RecordType recordType, FltReader reader) : base(parent, recordType, reader) {}

        public override void Parse()
        {
            Reader.BinaryParser.Position += 128;

            for (int i = 0; i < 1024; i++)
            {
                colors[i].argb = new byte[4];
                colors[i].argb[0] = Reader.BinaryParser.Byte();
                colors[i].argb[1] = Reader.BinaryParser.Byte();
                colors[i].argb[2] = Reader.BinaryParser.Byte();
                colors[i].argb[3] = Reader.BinaryParser.Byte();
            }

            if (Reader.RecordLength > 4228)
            {
                int colorNameCount = Reader.BinaryParser.Int32BE();
                for (int i = 0; i < colorNameCount; i++)
                {
                    short length = Reader.BinaryParser.Int16BE();

                    Reader.BinaryParser.Int16BE();

                    short index = Reader.BinaryParser.Int16BE();

                    Reader.BinaryParser.Int16BE();

                    if (index >= 0 && index < colors.Length && length > 8)
                        colors[index].name = Reader.BinaryParser.String(length - 8);
                }
            }
        }
    }

    public class MaterialPalette : Record
    {
        public int materialIndex;
        public string materialName = null;
        public Flags flags;
        public float[] ambient = new float[3]; // (r,g,b) 0.0-1.0
        public float[] diffuse = new float[3];
        public float[] specular = new float[3];
        public float[] emissive = new float[3];
        public float shininess; // 0.0-128.0
        public float alpha; // (a) 0.0-1.0

        public enum Flags
        {
            IsUsed = 0,
            //Spare = 1-31
        }

        public MaterialPalette(Record parent, RecordType recordType, FltReader reader) : base(parent, recordType, reader) {}

        public override void Parse()
        {
            materialIndex = Reader.BinaryParser.Int32BE();
            materialName = Reader.BinaryParser.String(12);
            flags = (Flags)Reader.BinaryParser.Int32BE();
            ambient[0] = Reader.BinaryParser.SingleBE();
            ambient[1] = Reader.BinaryParser.SingleBE();
            ambient[2] = Reader.BinaryParser.SingleBE();
            diffuse[0] = Reader.BinaryParser.SingleBE();
            diffuse[1] = Reader.BinaryParser.SingleBE();
            diffuse[2] = Reader.BinaryParser.SingleBE();
            specular[0] = Reader.BinaryParser.SingleBE();
            specular[1] = Reader.BinaryParser.SingleBE();
            specular[2] = Reader.BinaryParser.SingleBE();
            emissive[0] = Reader.BinaryParser.SingleBE();
            emissive[1] = Reader.BinaryParser.SingleBE();
            emissive[2] = Reader.BinaryParser.SingleBE();
            shininess = Reader.BinaryParser.SingleBE();
            alpha = Reader.BinaryParser.SingleBE();

            Reader.BinaryParser.Int32BE();
        }
    }

    public class TexturePalette : Record
    {
        public string filename = null;
        public int texturePatternIndex;
        public int locationX;
        public int locationY;

        public TexturePalette(Record parent, RecordType recordType, FltReader reader) : base(parent, recordType, reader) {}

        public override void Parse()
        {
            filename = Reader.BinaryParser.String(200);
            texturePatternIndex = Reader.BinaryParser.Int32BE();
            locationX = Reader.BinaryParser.Int32BE();
            locationY = Reader.BinaryParser.Int32BE();
        }
    }

    public class VertexPalette : Record
    {
        public Dictionary<int, VertexWithColor> Dict = new Dictionary<int, VertexWithColor>();
        public Dictionary<VertexWithColor, int> IndexDict = new Dictionary<VertexWithColor, int>();
        public List<VertexWithColor> List = new List<VertexWithColor>();
        public int Length = 0;
        public int Offset = 0;

        public VertexPalette(Record parent, RecordType recordType, FltReader reader) : base(parent, recordType, reader) {}

        public override void Parse()
        {
            Length = Reader.BinaryParser.Int32BE() - 8;


            Offset = 8;

            // Process vertices, etc.
            base.Parse();
        }
    }

    public class PushLevel : Record
    {
        public PushLevel(Record parent, RecordType recordType, FltReader reader) : base(parent, recordType, reader) {}

        public override void Parse()
        {
            base.Parse();
        }
    }

    public class PopLevel : Record
    {
        public PopLevel(Record parent, RecordType recordType, FltReader reader) : base(parent, recordType, reader) {}

        public override void Parse()
        {
        }
    }

    public class Matrix : Record
    {
        public float[,] matrix = new float[4, 4];

        public Matrix(Record parent, RecordType recordType, FltReader reader) : base(parent, recordType, reader) {}

        public override void Parse()
        {
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                    matrix[i, j] = Reader.BinaryParser.SingleBE();
            }
        }
    }

    public class GeneralMatrix : Record
    {
        public float[,] matrix = new float[4,4];

        public GeneralMatrix(Record parent, RecordType recordType, FltReader reader) : base(parent, recordType, reader) {}

        public override void Parse()
        {
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                    matrix[i, j] = Reader.BinaryParser.SingleBE();
            }
        }
    }

    public class Object : IdRecord
    {
        public Flags flags;
        public short relativePriority;
        public ushort transparency;
        public short specialEffectId1;
        public short specialEffectId2;
        public short significance;

        public enum Flags
        {
            NoDisplayDaylight = 0,
            NoDisplayDusk = 1,
            NoDisplayNight = 2,
            NoIlluminate = 3,
            FlatShader = 4,
            ShadowObject = 5,
            PreserveAtRuntime = 6,
            //Spare = 7-31
        }

        public Object(Record parent, RecordType recordType, FltReader reader) : base(parent, recordType, reader) {}

        public override void Parse()
        {
            idStr = Reader.BinaryParser.String(fixedIdLength);
            flags = (Flags)Reader.BinaryParser.Int32BE();
            relativePriority = Reader.BinaryParser.Int16BE();
            transparency = (ushort)Reader.BinaryParser.Int16BE();
            specialEffectId1 = Reader.BinaryParser.Int16BE();
            specialEffectId2 = Reader.BinaryParser.Int16BE();
            significance = Reader.BinaryParser.Int16BE();

            base.Parse();
        }
    }

    public class Group : IdRecord
    {
        public short relativePriority;
        public Flags flags;
        public short specialEffectId1;
        public short specialEffectId2;
        public short significance;
        public byte layerCode;
        public int loopCount;
        public float loopDurationInSeconds;
        public float lastFrameDurationInSeconds;

        public enum Flags
        {
            Reserved = 0,
            ForwardAnimation = 1,
            SwingAnimation = 2,
            BoundingBoxFollows = 3,
            FreezeBoundingBox = 4,
            DefaultParent = 5,
            BackwardAnimation = 6,
            PreserveAtRuntime = 7,
            //Spare = 8-31
        }

        public Group(Record parent, RecordType recordType, FltReader reader) : base(parent, recordType, reader) {}

        public override void Parse()
        {
            idStr = Reader.BinaryParser.String(fixedIdLength);
            relativePriority = Reader.BinaryParser.Int16BE();

            Reader.BinaryParser.Int16BE();

            flags = (Flags)Reader.BinaryParser.Int32BE();
            specialEffectId1 = Reader.BinaryParser.Int16BE();
            specialEffectId2 = Reader.BinaryParser.Int16BE();
            significance = Reader.BinaryParser.Int16BE();
            layerCode = Reader.BinaryParser.Byte();

            Reader.BinaryParser.Byte();
            Reader.BinaryParser.Int32BE();

            if (Root.Header.formatRevisionLevel < 1580)
            {
                base.Parse();
                return;
            }

            loopCount = Reader.BinaryParser.Int32BE();
            loopDurationInSeconds = Reader.BinaryParser.SingleBE();
            lastFrameDurationInSeconds = Reader.BinaryParser.SingleBE();

            base.Parse();
        }
    }

    public class Mesh : Record
    {
        public LocalVertexPool LocalVertexPool;
        public Submesh Submesh;

        public string idStr = null;
        public int irColorCode;
        public short relativePriority;
        public Face.DrawType drawType;
        public bool textureWhite;
        public ushort colorNameIndex;
        public ushort alternateColorNameIndex;
        public Face.Template template;
        public short detailTexturePatternIndex = -1;
        public short texturePatternIndex = -1;
        public short materialIndex = -1;
        public short surfaceMaterialCode; // for DFAD
        public short featureId; // for DFAD
        public int irMaterialCode;
        public ushort transparency;
        public byte lodGenerationControl;
        public byte lineStyleIndex;
        public Face.Flags flags;
        public Face.LightMode lightMode;
        public uint packedColorPrimary; // only b,g,r used
        public uint packedColorAlternate; // only b,g,r used
        public short textureMappingIndex = -1;
        public int primaryColorIndex = -1;
        public int alternateColorIndex = -1;
        public short shaderIndex = -1;

        public Mesh(Record parent, RecordType recordType, FltReader reader) : base(parent, recordType, reader) {}

        public override void Parse()
        {
            Reader.CurrentMeshRecord = this;
            idStr = Reader.BinaryParser.String(8);
            irColorCode = Reader.BinaryParser.Int32BE();
            Reader.BinaryParser.Position += 4;
            relativePriority = Reader.BinaryParser.Int16BE();
            drawType = (Face.DrawType)Reader.BinaryParser.Byte();
            if (drawType != Face.DrawType.DrawSolidWithBackfaceCulling && drawType != Face.DrawType.DrawSolidNoBackfaceCulling)
                Console.WriteLine("Face: unsupported draw type " + drawType.ToString());
            textureWhite = Reader.BinaryParser.Boolean();
            if (textureWhite)
                Console.WriteLine("Face: unsupported textureWhite");
            colorNameIndex = (ushort)Reader.BinaryParser.Int16BE();
            alternateColorNameIndex = (ushort)Reader.BinaryParser.Int16BE();
            Reader.BinaryParser.Position += 1;
            template = (Face.Template)Reader.BinaryParser.Byte();
            int detailTexturePatternIndex = Reader.BinaryParser.Int16BE();
            if (detailTexturePatternIndex != -1)
                Console.WriteLine("Face: detail texture pattern index specified");
            int texturePatternIndex = Reader.BinaryParser.Int16BE();
            int materialIndex = Reader.BinaryParser.Int16BE();
            surfaceMaterialCode = Reader.BinaryParser.Int16BE();
            featureId = Reader.BinaryParser.Int16BE();
            irMaterialCode = Reader.BinaryParser.Int32BE();
            transparency = (ushort)Reader.BinaryParser.Int16BE();
            lodGenerationControl = Reader.BinaryParser.Byte();
            lineStyleIndex = Reader.BinaryParser.Byte();
            flags = (Face.Flags)Reader.BinaryParser.Int32BE();
            lightMode = (Face.LightMode)Reader.BinaryParser.Byte();
            Reader.BinaryParser.Position += 7;
            packedColorPrimary = (uint)Reader.BinaryParser.Int32BE();
            packedColorAlternate = (uint)Reader.BinaryParser.Int32BE();
            textureMappingIndex = Reader.BinaryParser.Int16BE();
            Reader.BinaryParser.Int16BE();
            primaryColorIndex = Reader.BinaryParser.Int32BE();
            alternateColorIndex = Reader.BinaryParser.Int32BE();
            Reader.BinaryParser.Int16BE();

            shaderIndex = Reader.BinaryParser.Int16BE();

            var material = Root.GetOrAddMaterial((short)materialIndex, (short)texturePatternIndex, (short)detailTexturePatternIndex);
            Submesh = Root.GetOrAddSubmesh(material);
        }
    }

    public class LocalVertexPool : Record
    {
        public bool hasPosition = false;
        public bool hasColor = false;
        public bool hasRGBA = false;
        public bool hasNormal = false;
        public bool hasBaseUV = false;
        public bool hasUV1 = false;
        public bool hasUV2 = false;
        public bool hasUV3 = false;
        public bool hasUV4 = false;
        public bool hasUV5 = false;
        public bool hasUV6 = false;
        public bool hasUV7 = false;
        public List<LocalVertex> vertices = new List<LocalVertex>();
        public int paletteOffset = 0;

        public class LocalVertex
        {
            public double x, y, z;
            public uint color;
            public float i, j, k;
            public float u, v;
        }

        public LocalVertexPool(Record parent, RecordType recordType, FltReader reader) : base(parent, recordType, reader) {}

        public override void Parse()
        {
            if (Reader.CurrentMeshRecord != null)
                Reader.CurrentMeshRecord.LocalVertexPool = this;

            uint count = Reader.BinaryParser.UInt32BE();
            uint attr = Reader.BinaryParser.UInt32BE();
            hasPosition = (attr & (1 << 31)) > 0;
            hasColor = (attr & (1 << 30)) > 0;
            hasRGBA = (attr & (1 << 29)) > 0;
            hasNormal = (attr & (1 << 28)) > 0;
            hasBaseUV = (attr & (1 << 27)) > 0;
            hasUV1 = (attr & (1 << 26)) > 0;
            hasUV2 = (attr & (1 << 25)) > 0;
            hasUV3 = (attr & (1 << 24)) > 0;
            hasUV4 = (attr & (1 << 23)) > 0;
            hasUV5 = (attr & (1 << 22)) > 0;
            hasUV6 = (attr & (1 << 21)) > 0;
            hasUV7 = (attr & (1 << 20)) > 0;
            for (uint index = 0; index < count; ++index)
            {
                var vertex = new LocalVertex();
                if (hasPosition)
                {
                    vertex.x = Reader.BinaryParser.DoubleBE();
                    vertex.y = Reader.BinaryParser.DoubleBE();
                    vertex.z = Reader.BinaryParser.DoubleBE();
                }
                if (hasColor || hasRGBA)
                    vertex.color = Reader.BinaryParser.UInt32BE();
                if (hasNormal)
                {
                    vertex.i = Reader.BinaryParser.SingleBE();
                    vertex.j = Reader.BinaryParser.SingleBE();
                    vertex.k = Reader.BinaryParser.SingleBE();
                }
                if (hasBaseUV)
                {
                    vertex.u = Reader.BinaryParser.SingleBE();
                    vertex.v = Reader.BinaryParser.SingleBE();
                }
                if (hasUV1)
                {
                    Reader.BinaryParser.SingleBE();
                    Reader.BinaryParser.SingleBE();
                }
                if (hasUV2)
                {
                    Reader.BinaryParser.SingleBE();
                    Reader.BinaryParser.SingleBE();
                }
                if (hasUV3)
                {
                    Reader.BinaryParser.SingleBE();
                    Reader.BinaryParser.SingleBE();
                }
                if (hasUV4)
                {
                    Reader.BinaryParser.SingleBE();
                    Reader.BinaryParser.SingleBE();
                }
                if (hasUV5)
                {
                    Reader.BinaryParser.SingleBE();
                    Reader.BinaryParser.SingleBE();
                }
                if (hasUV6)
                {
                    Reader.BinaryParser.SingleBE();
                    Reader.BinaryParser.SingleBE();
                }
                if (hasUV7)
                {
                    Reader.BinaryParser.SingleBE();
                    Reader.BinaryParser.SingleBE();
                }
                vertices.Add(vertex);
            }

            paletteOffset = Root.VertexPalette.Dict.Count;
            for (int i = 0; i < vertices.Count; ++i)
            {
                var v = vertices[i];
                var vpv = new VertexWithColorNormalUV(null, RecordType.VertexWithColorNormalUv, null);
                vpv.x = v.x;
                vpv.y = v.y;
                vpv.z = v.z;
                vpv.i = v.i;
                vpv.j = v.j;
                vpv.k = v.k;
                vpv.u = v.u;
                vpv.v = v.v;
                Root.VertexPalette.Dict[Root.VertexPalette.Offset] = vpv;
                Root.VertexPalette.IndexDict[vpv] = Root.VertexPalette.List.Count;
                Root.VertexPalette.List.Add(vpv);
                Root.VertexPalette.Offset += 64;
            }
        }

    }

    public class MeshPrimitive : Record
    {
        public MeshPrimitive(Record parent, RecordType recordType, FltReader reader) : base(parent, recordType, reader) {}

        public override void Parse()
        {
            short type = Reader.BinaryParser.Int16BE();
            ushort indexSize = Reader.BinaryParser.UInt16BE();
            uint vertexCount = Reader.BinaryParser.UInt32BE();
            var indices = new int[vertexCount];
            for (uint i = 0; i < vertexCount; ++i)
            {
                if(indexSize == 1)
                    indices[i] = Reader.BinaryParser.Char();
                if(indexSize == 2)
                    indices[i] = Reader.BinaryParser.Int16BE();
                if(indexSize == 4)
                    indices[i] = Reader.BinaryParser.Int32BE();
            }

            var triangles = new int[(vertexCount - 2) * 3];
            if (type == 1)   // triangle strip
            {
                for (int i = 0; i + 2 < indices.Length; ++i)
                {
                    if (i % 2 == 1)
                    {
                        triangles[(i * 3) + 0] = indices[i + 1];
                        triangles[(i * 3) + 1] = indices[i + 0];
                        triangles[(i * 3) + 2] = indices[i + 2];
                    }
                    else
                    {
                        triangles[(i * 3) + 0] = indices[i + 0];
                        triangles[(i * 3) + 1] = indices[i + 1];
                        triangles[(i * 3) + 2] = indices[i + 2];
                    }
                }
            }
            if (type == 2)   // triangle fan
            {
                for (int i = 0; i + 2 < indices.Length; ++i)
                {
                    triangles[(i * 3) + 0] = indices[0];
                    triangles[(i * 3) + 1] = indices[i + 1];
                    triangles[(i * 3) + 2] = indices[i + 2];
                }
            }
            if (type == 3)   // quad strip
            {
                // TODO
                Console.WriteLine(string.Format("MeshPrimitive record type {0} not yet supported", type));
            }
            if (type == 4)  // indexed poly
            {
                // TODO
                Console.WriteLine(string.Format("MeshPrimitive record type {0} not yet supported", type));
            }

            if (Reader.CurrentMeshRecord != null)
            {
                var submesh = Reader.CurrentMeshRecord.Submesh;
                var offset = Reader.CurrentMeshRecord.LocalVertexPool.paletteOffset;
                for (int i = 0; i < triangles.Length; ++i)
                {
                    int val = triangles[i] + offset;
                    submesh.triangles.Add(val);
                    if(Reader.CurrentMeshRecord.drawType == Face.DrawType.DrawSolidNoBackfaceCulling)
                        submesh.backfaceTriangles.Add(val);
                }
            }
        }

    }

    public class Face : Record
    {
        public string idStr = null;
        public int irColorCode;
        public short relativePriority;
        public DrawType drawType;
        public bool textureWhite;
        public ushort colorNameIndex;
        public ushort alternateColorNameIndex;
        public Template template;
        public short detailTexturePatternIndex = -1;
        public short texturePatternIndex = -1;
        public short materialIndex = -1;
        public short surfaceMaterialCode; // for DFAD
        public short featureId; // for DFAD
        public int irMaterialCode;
        public ushort transparency;
        public byte lodGenerationControl;
        public byte lineStyleIndex;
        public Flags flags;
        public LightMode lightMode;
        public uint packedColorPrimary; // only b,g,r used
        public uint packedColorAlternate; // only b,g,r used
        public short textureMappingIndex = -1;
        public int primaryColorIndex = -1;
        public int alternateColorIndex = -1;
        public short shaderIndex = -1;

        public enum DrawType
        {
            DrawSolidWithBackfaceCulling = 0,
            DrawSolidNoBackfaceCulling = 1,
            DrawWireframeAndClose = 2,
            DrawWireframe = 3,
            SurroundWithWireFrameInAlternateColor = 4,
            OmnidirectionalLight = 8,
            UnidirectionalLight = 9,
            BidirectionalLight = 10,
        }

        public enum Template // billboard
        {
            FixedNoAlphaBlending = 0,
            FixedAlphaBlending = 1,
            AxialRotateWithAlphaBlending = 2,
            PointRotateWithAlphaBlending = 4,
        }

        public enum Flags
        {
            Terrain = 0,
            NoColor = 1,
            NoAlternateColor = 2,
            PackedColor = 3,
            TerrainCultureCutout = 4, // footprint
            HiddenNotDrawn = 5,
            Roofline = 6,
            //Spare = 7-31
        }

        public enum LightMode
        {
            UseFaceColorNotIlluminated = 0, // flat
            UseVertexColorsNotIlluminated = 1, // Gouraud
            UseFaceColorAndVertexNormals = 2, // lit
            UseVertexColorsAndVertexNormals = 3, // lit Gouraud
        }

        public Face(Record parent, RecordType recordType, FltReader reader) : base(parent, recordType, reader) {}

        public override void Parse()
        {
            idStr = Reader.BinaryParser.String(8);
            irColorCode = Reader.BinaryParser.Int32BE();
            relativePriority = Reader.BinaryParser.Int16BE();
            drawType = (DrawType)Reader.BinaryParser.Byte();
            if (drawType != DrawType.DrawSolidWithBackfaceCulling && drawType != DrawType.DrawSolidNoBackfaceCulling)
                Console.WriteLine("Face: unsupported draw type " + drawType.ToString());
            textureWhite = Reader.BinaryParser.Boolean();
            if (textureWhite)
                Console.WriteLine("Face: unsupported textureWhite");
            colorNameIndex = (ushort)Reader.BinaryParser.Int16BE();
            alternateColorNameIndex = (ushort)Reader.BinaryParser.Int16BE();

            Reader.BinaryParser.Byte();

            template = (Template)Reader.BinaryParser.Byte();
            //if (template != Template.FixedNoAlphaBlending)
            //    Console.WriteLine("Face: unsupported template " + template.ToString());
            detailTexturePatternIndex = Reader.BinaryParser.Int16BE();
            if (detailTexturePatternIndex != -1)
                Console.WriteLine("Face: detail texture pattern index specified");
            texturePatternIndex = Reader.BinaryParser.Int16BE();
            //if (texturePatternIndex == -1)
            //    Console.WriteLine("Face: no texture pattern index specified");
            materialIndex = Reader.BinaryParser.Int16BE();
            //if (materialIndex == -1)
            //    Console.WriteLine("Face: no material index specified");
            surfaceMaterialCode = Reader.BinaryParser.Int16BE();
            featureId = Reader.BinaryParser.Int16BE();
            irMaterialCode = Reader.BinaryParser.Int32BE();
            transparency = (ushort)Reader.BinaryParser.Int16BE();
            if (transparency != 0)
                Console.WriteLine("Face: unsupported transparency");
            lodGenerationControl = Reader.BinaryParser.Byte();
            lineStyleIndex = Reader.BinaryParser.Byte();
            flags = (Flags)Reader.BinaryParser.Int32BE();
            lightMode = (LightMode)Reader.BinaryParser.Byte();
            //if (lightMode != LightMode.UseVertexColorsNotIlluminated)
            //    Console.WriteLine("Face: unsupported lightMode " + lightMode.ToString());

            Reader.BinaryParser.Position += 7;

            packedColorPrimary = (uint)Reader.BinaryParser.Int32BE();
            packedColorAlternate = (uint)Reader.BinaryParser.Int32BE();
            textureMappingIndex = Reader.BinaryParser.Int16BE();

            Reader.BinaryParser.Int16BE();

            primaryColorIndex = Reader.BinaryParser.Int32BE();
            alternateColorIndex = Reader.BinaryParser.Int32BE();

            Reader.BinaryParser.Int16BE();

            if (Root.Header.formatRevisionLevel < 1600)
            {
                base.Parse();
                return;
            }

            shaderIndex = Reader.BinaryParser.Int16BE();

            base.Parse();
        }
    }

    public class LevelOfDetail : IdRecord
    {
        public double switchInDistance;
        public double switchOutDistance;
        public short specialEffectId1;
        public short specialEffectId2;
        public Flags flags;
        public double centerX, centerY, centerZ;
        public double transitionRange;
        public const float multiplier = 1222.310099f; // TODO: this (presumably) assumes 30 degree FOV with 1920 width (2*1920/PI=1222.3)

        // 15.8 - significantSize is used to calculate switch in and out distances based on FOV, screen size, resolution
        public double significantSize;

        public enum Flags
        {
            UsePreviousSlantRange = 0,
            AdditiveLodsBelow = 1,
            FreezeCenter = 2, // freeze center (don't recalculate)
            //Spare = 3-31
        }

        public LevelOfDetail(Record parent, RecordType recordType, FltReader reader) : base(parent, recordType, reader) {}

        public override void Parse()
        {
            idStr = Reader.BinaryParser.String(fixedIdLength);

            Reader.BinaryParser.Int32BE();

            switchInDistance = Reader.BinaryParser.DoubleBE();
            switchOutDistance = Reader.BinaryParser.DoubleBE();
            specialEffectId1 = Reader.BinaryParser.Int16BE();
            specialEffectId2 = Reader.BinaryParser.Int16BE();
            flags = (Flags)Reader.BinaryParser.Int32BE();
            centerX = Reader.BinaryParser.DoubleBE();
            centerY = Reader.BinaryParser.DoubleBE();
            centerZ = Reader.BinaryParser.DoubleBE();
            transitionRange = Reader.BinaryParser.DoubleBE();

            if (Root.Header.formatRevisionLevel < 1580)
            {
                base.Parse();
                return;
            }

            significantSize = Reader.BinaryParser.DoubleBE();
            if (switchInDistance == 0f && switchOutDistance == 0f)
            {
                // TODO: at the UnityCDB level, we will need to deal with our FOV and screen resolution to compute the correct value
                //switchInDistance = significantSize * multiplier;
                //switchOutDistance = 2f * switchInDistance;
            }

            base.Parse();
        }
    }

    public class VertexList : Record
    {
        public int[] offsets = null;

        public VertexList(Record parent, RecordType recordType, FltReader reader) : base(parent, recordType, reader) {}

        public override void Parse()
        {
            int count = (Reader.RecordLength - 4) / sizeof(int);
            offsets = new int[count];
            for (int i = 0; i < count; i++)
                offsets[i] = Reader.BinaryParser.Int32BE();

            var idParent = FindParentIdRecord();
            if (idParent != null)
            {
                if (idParent.Submeshes == null)
                    idParent.InitGeometry();

                if (offsets.Length == 3)
                {
                    var face = FindParentFace();
                    var idRecord = FindParentIdRecord();
                    var material = Root.GetOrAddMaterial(face);
                    var submesh = idRecord.GetOrAddSubmesh(material);
                    foreach (var offset in offsets)
                    {
                        VertexWithColor vert = null;
                        if (Root.VertexPalette.Dict.TryGetValue(offset, out vert))
                        {
                            int index = -1;
                            if (Root.VertexPalette.IndexDict.TryGetValue(vert, out index))
                            {
                                submesh.triangles.Add(index);
                                if (face.drawType == Face.DrawType.DrawSolidNoBackfaceCulling)
                                    submesh.backfaceTriangles.Add(index);
                            }
                        }
                    }
                }
                else
                {
                    // TODO: handle triangulation on polygons
                    Console.WriteLine(string.Format("polygon found with {0} vertices. These are not handled yet.", offsets.Length));
                }
            }
        }
    }

    public class VertexWithColor : Record
    {
        public ushort colorNameIndex;
        public Flags flags;
        public double x, y, z;
        public byte packedIndexA;
        public byte packedIndexB;
        public byte packedIndexG;
        public byte packedIndexR;
        public uint colorIndex;

        public enum Flags
        {
            StartHardEdge = 0,
            NormalFrozen = 1,
            NoColor = 2,
            PackedColor = 3,
            //Spare = 4-15
        }

        public VertexWithColor(Record parent, RecordType recordType, FltReader reader) : base(parent, recordType, reader) {}

        public override void Parse()
        {
            colorNameIndex = (ushort)Reader.BinaryParser.Int16BE();
            flags = (Flags)Reader.BinaryParser.Int16BE();
            x = Reader.BinaryParser.DoubleBE();
            y = Reader.BinaryParser.DoubleBE();
            z = Reader.BinaryParser.DoubleBE();
            packedIndexA = Reader.BinaryParser.Byte();
            packedIndexB = Reader.BinaryParser.Byte();
            packedIndexG = Reader.BinaryParser.Byte();
            packedIndexR = Reader.BinaryParser.Byte();
            colorIndex = (uint)Reader.BinaryParser.Int32BE();

            var vertexPalette = Root.VertexPalette;
            vertexPalette.Dict[vertexPalette.Offset] = this;
            vertexPalette.IndexDict[this] = vertexPalette.List.Count;
            vertexPalette.List.Add(this);
            vertexPalette.Offset += Reader.RecordLength;
        }
    }

    public class VertexWithColorNormal : VertexWithColor
    {
        public float i, j, k;

        public VertexWithColorNormal(Record parent, RecordType recordType, FltReader reader) : base(parent, recordType, reader) {}

        public override void Parse()
        {
            colorNameIndex = (ushort)Reader.BinaryParser.Int16BE();
            flags = (Flags)Reader.BinaryParser.Int16BE();
            x = Reader.BinaryParser.DoubleBE();
            y = Reader.BinaryParser.DoubleBE();
            z = Reader.BinaryParser.DoubleBE();
            i = Reader.BinaryParser.SingleBE();
            j = Reader.BinaryParser.SingleBE();
            k = Reader.BinaryParser.SingleBE();
            packedIndexA = Reader.BinaryParser.Byte();
            packedIndexB = Reader.BinaryParser.Byte();
            packedIndexG = Reader.BinaryParser.Byte();
            packedIndexR = Reader.BinaryParser.Byte();
            colorIndex = (uint)Reader.BinaryParser.Int32BE();

            var vertexPalette = Root.VertexPalette;
            vertexPalette.Dict[vertexPalette.Offset] = this;
            vertexPalette.IndexDict[this] = vertexPalette.List.Count;
            vertexPalette.List.Add(this);
            vertexPalette.Offset += Reader.RecordLength;
        }
    }

    public class VertexWithColorUV : VertexWithColor
    {
        public float u, v;

        public VertexWithColorUV(Record parent, RecordType recordType, FltReader reader) : base(parent, recordType, reader) {}

        public override void Parse()
        {
            colorNameIndex = (ushort)Reader.BinaryParser.Int16BE();
            flags = (Flags)Reader.BinaryParser.Int16BE();
            x = Reader.BinaryParser.DoubleBE();
            y = Reader.BinaryParser.DoubleBE();
            z = Reader.BinaryParser.DoubleBE();
            u = Reader.BinaryParser.SingleBE();
            v = Reader.BinaryParser.SingleBE();
            packedIndexA = Reader.BinaryParser.Byte();
            packedIndexB = Reader.BinaryParser.Byte();
            packedIndexG = Reader.BinaryParser.Byte();
            packedIndexR = Reader.BinaryParser.Byte();
            colorIndex = (uint)Reader.BinaryParser.Int32BE();

            var vertexPalette = Root.VertexPalette;
            vertexPalette.Dict[vertexPalette.Offset] = this;
            vertexPalette.IndexDict[this] = vertexPalette.List.Count;
            vertexPalette.List.Add(this);
            vertexPalette.Offset += Reader.RecordLength;
        }
    }

    public class VertexWithColorNormalUV : VertexWithColor
    {
        public float i, j, k;
        public float u, v;

        public VertexWithColorNormalUV(Record parent, RecordType recordType, FltReader reader) : base(parent, recordType, reader) {}

        public override void Parse()
        {
            colorNameIndex = (ushort)Reader.BinaryParser.Int16BE();
            flags = (Flags)Reader.BinaryParser.Int16BE();
            x = Reader.BinaryParser.DoubleBE();
            y = Reader.BinaryParser.DoubleBE();
            z = Reader.BinaryParser.DoubleBE();
            i = Reader.BinaryParser.SingleBE();
            j = Reader.BinaryParser.SingleBE();
            k = Reader.BinaryParser.SingleBE();
            u = Reader.BinaryParser.SingleBE();
            v = Reader.BinaryParser.SingleBE();
            packedIndexA = Reader.BinaryParser.Byte();
            packedIndexB = Reader.BinaryParser.Byte();
            packedIndexG = Reader.BinaryParser.Byte();
            packedIndexR = Reader.BinaryParser.Byte();
            colorIndex = (uint)Reader.BinaryParser.Int32BE();

            var vertexPalette = Root.VertexPalette;
            vertexPalette.Dict[vertexPalette.Offset] = this;
            vertexPalette.IndexDict[this] = vertexPalette.List.Count;
            vertexPalette.List.Add(this);
            vertexPalette.Offset += Reader.RecordLength;
        }
    }

    public class ExternalReference : Record
    {
        public string path = null;
        public Flags flags;
        public short viewAsBoundingBox;

        public enum Flags
        {
            ColorPaletteOverride = 0,
            MaterialPaletteOverride = 1,
            TextureAndTextureMappingPaletteOverride = 2,
            LineStylePaletteOverride = 3,
            SoundPaletteOverride = 4,
            LightSourcePaletteOverride = 5,
            LightPointPaletteOverride = 6,
            ShaderPaletteOverride = 7,
            //Spare = 8-31
        }

        public ExternalReference(Record parent, RecordType recordType, FltReader reader) : base(parent, recordType, reader) {}

        public override void Parse()
        {
            path = Reader.BinaryParser.String(200);

            Reader.BinaryParser.Int32BE();

            flags = (Flags)Reader.BinaryParser.Int32BE();
            viewAsBoundingBox = Reader.BinaryParser.Int16BE();

            Reader.BinaryParser.Int16BE();

            var dbPath = Path.GetDirectoryName(Root.Path);
            path = Path.Combine(dbPath, path);
            if (File.Exists(path))
            {
                path = Path.GetFullPath(path);
                //Console.WriteLine(string.Format("External reference for {0} at {1}", Root.Path, path));
                var bytes = System.IO.File.ReadAllBytes(path);
                var parser = new Cognitics.BinaryParser(bytes);
                var reader = new FltReader(parser);
                var fltDB = new FltDatabase(this, RecordType.Invalid, reader, path);
                fltDB.Parse();
                Root.ExternalDBs.Add(fltDB);
            }

            base.Parse();
        }
    }

    //public class ExtendedMaterialHeader : Record
    //{
    //    public ExtendedMaterialHeader(Record parent, RecordType recordType, FltReader reader) : base(parent, recordType, reader) {}

    //    public override void Parse()
    //    {
    //        Console.WriteLine(string.Format("found {0} in {1}", RecordType.ToString(), Root.Path));
    //    }
    //}

    //public class MultiTexture : Record
    //{
    //    public MultiTexture(Record parent, RecordType recordType, FltReader reader) : base(parent, recordType, reader) {}

    //    public override void Parse()
    //    {
    //        Console.WriteLine(string.Format("found {0} in {1}", RecordType.ToString(), Root.Path));
    //    }
    //}

    //public class UvList : Record
    //{
    //    public UvList(Record parent, RecordType recordType, FltReader reader) : base(parent, recordType, reader) {}

    //    public override void Parse()
    //    {
    //        Console.WriteLine(string.Format("found {0} in {1}", RecordType.ToString(), Root.Path));
    //    }
    //}

    #endregion



    public class Texture
    {
        public int Width = 0;
        public int Height = 0;
        public int NumChannels = 0;
        public byte[] rgb = null;
        public string Path = null;

        public Texture(string path)
        {
            Path = path;
        }

        public void Parse(byte[] bytes = null)
        {
            if (Path.EndsWith(".rgb"))
            {
                var texture = new CDB.SiliconGraphicsImage();
                rgb = texture.ReadRGB8(Path, bytes, out Width, out Height, out NumChannels, true);
            }
            // TODO: haven't found where in the spec these are actually dealt with (they never seem to be main/detail texture), and the code is preliminary
            else if (Path.EndsWith(".tif"))
            {
                // TODO: read mesh type from channel 1

                string logname = "OpenFlight.Texture.Parse(): " + Path;

                var tiff = Tiff.Open(Path, "r");
                if (tiff == null)
                {
                    Console.WriteLine(logname + ": Tiff.Open() failed");
                    return;// null;
                }
                FieldValue[] value = tiff.GetField(TiffTag.IMAGEWIDTH);
                Width = value[0].ToInt();
                value = tiff.GetField(TiffTag.IMAGELENGTH);
                Height = value[0].ToInt();
                {
                    FieldValue[] bitDepth = tiff.GetField(TiffTag.BITSPERSAMPLE);
                    FieldValue[] dataTypeTag = tiff.GetField(TiffTag.SAMPLEFORMAT);
                    int bpp = bitDepth[0].ToInt();
                    NumChannels = bpp / 8;
                    int dataType = dataTypeTag[0].ToInt();

                    int stride = tiff.ScanlineSize();
                    byte[] buffer = new byte[stride];

                    var result = new byte[Width * Height];

                    for (int row = 0; row < Height; ++row)
                    {
                        if (!tiff.ReadScanline(buffer, row))
                        {
                            Console.WriteLine(logname + ": Tiff.ReadScanLine(buffer, " + row.ToString() + ") failed");
                            break;
                        }

                        /*// Case of float
                        if (bpp == 32 && dataType == 3)
                        {
                            for (int col = 0; col < Width; ++col)
                                result[(row * Width) + col] = BitConverter.ToSingle(buffer, col * 4);
                        }
                        // case of Int32
                        else if (bpp == 32 && dataType == 2)
                        {
                            for (int col = 0; col < Width; ++col)
                                result[(row * Width) + col] = BitConverter.ToInt32(buffer, col * 4);
                        }
                        // Case of Int16
                        else if (bpp == 16 && dataType == 2)
                        {
                            for (int col = 0; col < Width; ++col)
                                result[(row * Width) + col] = BitConverter.ToInt16(buffer, col * 2);
                        }
                        // Case of Int8
                        else*/
                        //{
                        if (bpp == 8 && (dataType == 1 || dataType == 2))
                        {
                            for (int col = 0; col < Width; ++col)
                                result[(row * Width) + col] = buffer[col];
                        }
                        //}
                        // Case of Unknown Datatype
                        else
                        {
                            // TODO: fix log. it should be current with what we support.
                            Console.WriteLine(
                                logname +
                                ": Unknown Tiff file format " +
                                "(bits per pixel:" + bpp.ToString() +
                                ",  dataType code: " + dataType.ToString() +
                                "). Expected bpp values: 8, 16, or 32. Expected dataType values: 1 (two's complement signed int), or 3 (IEEE float)."
                                );
                        }
                    }

                    rgb = result;
                }
            }
        }
    }







}

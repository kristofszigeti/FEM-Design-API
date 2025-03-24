using FemDesign;
using FemDesign.Loads;
using FemDesign.Geometry;
using System;
using System.Collections.Specialized;
using System.Net.Mail;
using FemDesign.Results;

var L = 6.0;

var beamLength = new List<double>(){3,4,5,6};

foreach(var length in beamLength)
{
var point1 = new FemDesign.Geometry.Point3d(0,0,0);
var point2 = new FemDesign.Geometry.Point3d(length,0,0);

var edge = new FemDesign.Geometry.Edge(point1, point2);

var motions = new FemDesign.Releases.Motions(
    FemDesign.Releases.Motions.ValueRigidPoint, 
    FemDesign.Releases.Motions.ValueRigidPoint, 
    FemDesign.Releases.Motions.ValueRigidPoint, 
    FemDesign.Releases.Motions.ValueRigidPoint, 
    FemDesign.Releases.Motions.ValueRigidPoint, 
    FemDesign.Releases.Motions.ValueRigidPoint);

var rotations = new FemDesign.Releases.Rotations(0,0,0,0,0,0);

var support1 = new FemDesign.Supports.PointSupport(point1, motions, rotations);
var support2 = new FemDesign.Supports.PointSupport(point2, true, true, true, true, true, true);

// var rigidSupport = FemDesign.Supports.PointSupport.Rigid(point1, motions, rotations);

// load cases
var ldDL = new FemDesign.Loads.LoadCase("DL",FemDesign.Loads.LoadCaseType.DeadLoad, FemDesign.Loads.LoadCaseDuration.Permanent);
var ldLL = new FemDesign.Loads.LoadCase("LL",FemDesign.Loads.LoadCaseType.Static, FemDesign.Loads.LoadCaseDuration.Permanent);
var ldWind = new FemDesign.Loads.LoadCase("WIND",FemDesign.Loads.LoadCaseType.Static, FemDesign.Loads.LoadCaseDuration.Permanent);


// load combination
var loadComb1 = new FemDesign.Loads.LoadCombination(
    "ULS", 
    FemDesign.Loads.LoadCombType.UltimateOrdinary,
    (ldDL, 1.3),
    (ldLL, 1.5),
    (ldWind, 1.5));

// create loads
var pointLoad = new FemDesign.Loads.PointLoad
                    (
                        new FemDesign.Geometry.Point3d(L/2, 0, 0),
                        new FemDesign.Geometry.Vector3d(0,0,-10),
                        ldLL,
                        "pointLoad",
                        FemDesign.Loads.ForceLoadType.Force
                    );

var LineForce = new FemDesign.Geometry.Vector3d(0,-20,0);
var lineLoad = new FemDesign.Loads.LineLoad(
                                            edge: edge,
                                            constantForce: LineForce,
                                            loadCase: ldWind,
                                            loadType: FemDesign.Loads.ForceLoadType.Force,
                                            "Wind"
                                            );

var loads = new List<FemDesign.GenericClasses.ILoadElement>(){pointLoad, lineLoad};

var materialDatabase = FemDesign.Materials.MaterialDatabase.GetDefault();
var material = materialDatabase.MaterialByName("S 355"); 

var sectionDatabase = FemDesign.Sections.SectionDatabase.GetDefault();
var section = sectionDatabase.SectionByName("HEA300");

var beam = new FemDesign.Bars.Beam(edge, material, section);
var model = new FemDesign.Model(Country.H);

model.AddSupports(support1, support2);
model.AddElements(beam);
model.AddLoadCases(ldDL, ldLL, ldWind);
model.AddLoadCombinations(loadComb1);
//model.AddLoads(pointLoad);
//model.AddLoads(lineLoad);
model.AddLoads(loads);

var units = FemDesign.Results.UnitResults.Default();
units.Displacement = FemDesign.Results.Displacement.mm;
units.Force = FemDesign.Results.Force.kN;

using (var connection = new FemDesign.FemDesignConnection( keepOpen: true))
    {
    connection.Open(model);

    var analysis = FemDesign.Calculate.Analysis.StaticAnalysis();
    connection.RunAnalysis(analysis);

    var nodalDisplacement = connection.GetLoadCombinationResults<FemDesign.Results.NodalDisplacement>(units: units);

    var beamForces = connection.GetLoadCaseResults<FemDesign.Results.BarInternalForce>(units: units);

    var maxDisplacement = nodalDisplacement.Select(x => Math.Abs(x.Ez)).Max(); 

    var maxBending = beamForces.Select(x => x.Mz).Max();
    var minBending = beamForces.Select(x => x.Mz).Min();

    Console.WriteLine("Hi, Budapest!");
    Console.WriteLine("-------------");
    Console.WriteLine($"BeamLength: {beamLength}");
    Console.WriteLine($"Max displacement: {maxDisplacement} mm");
    Console.WriteLine($"Max Bending moment is: {maxBending} kNm");
    Console.WriteLine($"Max Bending moment is: {minBending} kNm");
    
    
    
    }
}

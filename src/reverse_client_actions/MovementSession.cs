using System;
using GrimDynasty.ReverseClient.Actions;

// Repeatable movement session over the recovered contracts.
//
// This is the same wiring the verified two-frame test uses, with the assertions
// removed so a host can drive it interactively. It re-creates the fixture per
// request (a fresh recovered request/Begin) and then steps bounded frames.
// Nothing here is a substitute for the original client: the scene is the pinned
// saved mainmenu member and every unknown input is published explicitly.
public sealed class MovementSession : IDisposable
{
    readonly NavigationCrowdAvoidanceConstants constants = new NavigationCrowdAvoidanceConstants();
    readonly NavigationCrowdPointerDomain<NavigationCrowdAgent> domain = new NavigationCrowdPointerDomain<NavigationCrowdAgent>();
    readonly SystemUcrtRotationMath math;

    public readonly NavigationSearchWrapperTests.Scene Scene;
    public readonly GameMoveBeginResourceIntegrationTests.Fixture Move;
    public readonly PlayerFramePublicationFixture Publication;
    readonly NavigationCrowdTickProductionBoundaries tick;
    readonly NavigationCrowdTickPrefixBoundaries boundary;
    public readonly NavigationCrowdTickPrefixInvocation Prefix = new NavigationCrowdTickPrefixInvocation();
    public readonly GameMovedReachedTailInvocation Tail = new GameMovedReachedTailInvocation();
    public int Frame { get; private set; }
    public bool RequestReturned { get; private set; }
    public float[] Target { get; private set; }
    public float RequestedTargetX { get; private set; }
    public float RequestedTargetZ { get; private set; }

    public MovementSession(float targetX, float targetZ)
    {
        math = SystemUcrtRotationMath.OpenOwnProfile();
        Scene = new NavigationSearchWrapperTests.Scene();
        constants.UseObservedDiskConstants();
        WritePoint(constants.Fallback422980, 0, 0, 0);
        Action<PlayerMoveRequestTests.Fixture> positions = delegate(PlayerMoveRequestTests.Fixture owner)
        {
            GameMoveBeginGroundIntegrationTests.InitializePositions(owner, Scene);
        };
        Move = new GameMoveBeginResourceIntegrationTests.Fixture(Scene, false, positions, constants.Fallback422980);
        Move.Ready();
        var m = Move.Movement;
        var r = Move.Request;
        m.Crowd.CriticalA0.Write(new NavigationCrowdCriticalSection());
        m.Agent.RequestE8.InstallOwnRefStorage(new NavigationRefBuffer(1024), 0);
        new NavigationCrowdWaypointVector(m.Agent.RequestE8.WaypointVector38).InstallOwnStorage(new NavigationPointBuffer(1024), 0);
        var costs = new NavigationCorridorFilterCosts(m.Query.Filter18);
        for (int n = 0; n < 64; n++) costs.AreaCosts00[n].Write(1);
        Scene.Search.Attach(costs);
        var state = new NavigationCrowdStateBoundaries(m.Movement.Adapter, constants.Fallback422980);
        state.UseObservedGroupConstants();
        state.DisableAllAssert42144A.Write(0);
        var proximity = new NavigationCrowdProximityBoundaries(state);
        proximity.DisableNoFreeAssert422BA0.Write(0);
        var path = m.Movement.Path;
        path.UseObservedPathConstants();
        path.UseObservedStraightPathConstants();
        path.UseObservedEqualityConstant();
        path.UseObservedDiskNoOp00CE70OwnAdapter();
        path.DisableAllAssert42144A.Write(0);
        path.DisableSecondAssert422B92.Write(0);
        path.DisableTrimAssert422B93.Write(0);
        path.DisablePopAssert422B94.Write(0);
        tick = new NavigationCrowdTickProductionBoundaries(path, proximity, m.Movement.Adapter, constants, math);
        tick.DisableFirstAssert422B91.Write(0);
        tick.DisableHeadingAssert422B9C.Write(0);
        var ray = new NavigationCrowdRaycastBoundaries(Scene.Mesh);
        ray.UseObservedRaycastConstants();
        boundary = new NavigationCrowdTickPrefixBoundaries(tick, ray);
        boundary.UseObservedDiskConstants();
        boundary.Global4228C0.X.Write(0);
        boundary.Global4228C0.Y.Write(0);
        boundary.Global4228C0.Z.Write(0);
        Publication = new PlayerFramePublicationFixture(Move, Scene, math);
        var sound = GameRunningSoundState.ForOwner(r.Owner);
        sound.Sound2210.Write(null);
        sound.Sound2110.Write(null);
        m.Composition.Production.BindRunningSound(new GameRunningSoundContext(r.Bridge));

        // The first recovered request/Begin is issued by RequestMove so that a caller
        // can choose the ground target. Issuing it here and again later fails at
        // 0x1662D0, so each session carries exactly one request.
        RequestedTargetX = targetX;
        RequestedTargetZ = targetZ;
        // One request per session, issued here (the position the fixture published).
        byte result = r.Outer(Move.Outer);
        RequestReturned = result == 1;
        Target = Values(m.Agent.Target78);
    }

    public void Step()
    {
        var m = Move.Movement;
        Move.Request.EngineLifetime.FrameCount640.Write((uint)(100 + Frame));
        Move.Request.Owner.Entity.LastFrame178.Write((uint)(99 + Frame));
        NavigationCrowdTickPrefixContracts.RunSingleReady25C440(m.Group, m.Agent, domain, 50, boundary, Publication.Moved, Prefix);
        GameMovedReachedTailContracts.ProduceInvokeAndFinishReachedLastWork(
            m.Group, m.Agent, domain, Prefix.MaximumRadius, Prefix.NeighborFactor, Prefix.Seconds,
            50, tick, Publication.Moved, Tail, null);
        Frame++;
    }

    // Re-targets the SAME session at a new ground point, mirroring the fixture's
    // GroundPosition support step (region token 77, ray probe height 29.5, then
    // the saved ground height through PutOnFloor22C620) and re-running the
    // recovered request/Begin. Returns true when the request returned 1.
    public bool RequestMove(float worldX, float worldZ)
    {
        var r = Move.Request;
        var position = r.Position;
        position.Q0.Write(77);
        position.D8.Write(Bits(worldX));
        position.D10.Write(Bits(worldZ));
        position.DC.Write(Bits(29.5f));
        byte floor = WorldFloorContracts.PutOnFloor22C620(position, r.World, Scene.Floor, new Invocation());
        if (floor != 1) { RequestReturned = false; return false; }
        byte result = r.Outer(Move.Outer);
        RequestReturned = result == 1;
        Target = Values(Move.Movement.Agent.Target78);
        Frame = 0;
        return RequestReturned;
    }

    public float[] PublishedPosition()
    {
        var p = Move.Request.Owner.Entity.CoordsD8.Position00;
        return new[] { Flt(p.D8.Read(0)), Flt(p.DC.Read(0)), Flt(p.D10.Read(0)) };
    }

    public static void WritePoint(NavigationPoint3 p, float x, float y, float z) { p.X.Write(x); p.Y.Write(y); p.Z.Write(z); }
    public static float[] Values(NavigationPoint3 p) { return new[] { p.X.Read(0), p.Y.Read(0), p.Z.Read(0) }; }
    static float Flt(uint bits) { return BitConverter.ToSingle(BitConverter.GetBytes(bits), 0); }
    static uint Bits(float v) { return BitConverter.ToUInt32(BitConverter.GetBytes(v), 0); }

    public void Dispose() { if (math != null) math.Dispose(); }
}

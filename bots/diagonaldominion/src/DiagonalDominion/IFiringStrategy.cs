namespace DiagonalDominion;

public interface IFiringStrategy
{
    Coordinate GetNextShot();
    void Reset();
    void RecordHit(Coordinate coordinate);
    void RecordMiss(Coordinate coordinate);
    void RecordSunk();
}

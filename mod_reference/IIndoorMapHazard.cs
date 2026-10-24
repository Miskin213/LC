public interface IIndoorMapHazard
{
	bool IsActivated();

	bool IsExtremelyHazardous();

	IndoorMapHazardType GetMapHazardType();
}

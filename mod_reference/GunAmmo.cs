public class GunAmmo : GrabbableObject
{
	public int ammoType;

	protected override void __initializeVariables()
	{
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		base.__initializeRpcs();
	}

	protected internal override string __getTypeName()
	{
		return "GunAmmo";
	}
}

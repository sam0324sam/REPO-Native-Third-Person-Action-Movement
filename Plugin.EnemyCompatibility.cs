using UnityEngine;
using Object = UnityEngine.Object;

namespace RepoThirdPerson;

public sealed partial class Plugin
{
	internal void EnableEnemyOnScreenDuringThirdPerson(EnemyOnScreen enemyOnScreen)
	{
		if ((Object)(object)enemyOnScreen == (Object)null)
		{
			return;
		}

		enemyOnScreen.enableOnCameraOverride = true;
	}
}

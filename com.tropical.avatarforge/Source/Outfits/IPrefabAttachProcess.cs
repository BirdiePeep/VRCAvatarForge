using UnityEngine;

namespace Tropical.AvatarForge
{
	public interface IPrefabAttachProcess
	{
		void OnAttach(AvatarForge setup, GameObject target);
		void OnRemove(AvatarForge setup, GameObject target);
	}
}
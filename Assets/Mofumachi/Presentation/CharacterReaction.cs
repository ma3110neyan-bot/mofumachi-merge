using System.Collections;
using UnityEngine;

namespace Mofumachi.Presentation
{
    // Moves the approved portrait frame only; no invented face or walk sprites.
    public sealed class CharacterReaction : MonoBehaviour
    {
        private Coroutine reaction;
        private RoundedPanelGraphic frame;
        private void Awake() => frame = GetComponent<RoundedPanelGraphic>();
        public void React()
        {
            if (!isActiveAndEnabled) return;
            foreach (var other in transform.parent.GetComponentsInChildren<CharacterReaction>())
                if (other != this) other.ClearSelection();
            frame.color = UIWidgets.Pink; frame.SetVerticesDirty();
            if (reaction != null) StopCoroutine(reaction);
            reaction = StartCoroutine(Bounce());
        }
        private IEnumerator Bounce()
        {
            float elapsed = 0;
            while (elapsed < .4f)
            {
                elapsed += Time.unscaledDeltaTime;
                float wave = Mathf.Sin(Mathf.Clamp01(elapsed / .4f) * Mathf.PI);
                transform.localScale = Vector3.one * (1 + .075f * wave);
                transform.localRotation = Quaternion.Euler(0, 0, -4 * wave);
                yield return null;
            }
            transform.localScale = Vector3.one; transform.localRotation = Quaternion.identity;
            reaction = null;
        }
        private void ClearSelection() { frame.color = UIWidgets.Cream; frame.SetVerticesDirty(); }
        private void OnDisable()
        { StopAllCoroutines(); reaction = null; transform.localScale = Vector3.one; transform.localRotation = Quaternion.identity; }
    }
}

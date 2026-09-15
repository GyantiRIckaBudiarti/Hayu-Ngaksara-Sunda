using UnityEngine;

namespace HayuNgaksara
{
    public class GuruController : CharacterBase
    {
        public void GiveInstruction(string text)
        {
            ShowDialog(text, 3f);
        }

        public void ReactToCorrect()
        {
            SetExpression(Expression.Happy, 1.5f);
        }

        public void ReactToWrong()
        {
            SetExpression(Expression.Sad, 1.5f);
        }
    }
}

using Godot;
namespace HandleInput{
    public class InputHandler{
        public Vector3 GetInputVector(bool Forward, bool Back, bool Left, bool Right){
            Vector3 direction = new Vector3(
                (Forward?1:0) - (Back?1:0),
                0,
                (Right?1:0) - (Left?1:0)
            ).Normalized();

            return direction;

        }
    }
}
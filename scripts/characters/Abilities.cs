using Godot;

public abstract partial class Ability : Node2D {
    public Fighters Fighter { get; private set; }
    public bool Active { get; protected set; }          // true = ability controls the fighter
    public virtual bool Invulnerable => false;

    public void Bind(Fighters fighter) => Fighter = fighter;

    public abstract void OnPressed();                    // ability button pressed
    public abstract void Tick(float delta);              // every physics frame while Active
    public virtual void Cancel() { Active = false; }     // interrupted (hit, KO)
}
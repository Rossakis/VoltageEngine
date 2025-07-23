using Nez;
using Nez.Sprites;

namespace Nez.Utils;

public class AnimationUtils
{
    /// <summary>
    /// Example for naming animations: If aseprite animation has tag "Idle", but you want it to be called by a different name in the animator (e.g. "Character-Idle")
    /// then the method call would be LoadAsepriteAnimation(animator, asepriteFile, "Idle", "Character-Idle").
    /// <para> If <c>callableAnimationName</c> is not changed, then the <c>animationTagName</c>  will be given to the animation instead (useful if if animation tag and animation name have always the same name)</para>
    /// </summary>
    /// <param name="animationTagName">The name of the tag containing the desired animation in the aseprite file.</param>
    /// <param name="callableAnimationName">The name we want to give the animation, which we will need to call when using Animator.Play("animation")</param>
    /// <param name="layerName">If left as null, then animator will select ALL layers for the animation.</param>
    public static void LoadAsepriteAnimation(Entity entity, string asepriteFilePath, string animationTagName, string callableAnimationName = null, string layerName = null)
    {
        SpriteAtlas sprite = entity.Scene.Content.LoadAsepriteFile(asepriteFilePath).ToSpriteAtlas(layerName);

        if (callableAnimationName == null) // animation name not assigned
            entity.GetComponent<SpriteAnimator>().AddAnimation(animationTagName, sprite.GetAnimation(animationTagName));
        else
            entity.GetComponent<SpriteAnimator>().AddAnimation(callableAnimationName, sprite.GetAnimation(animationTagName));
    }

    /// <summary>
    /// Create animation only based on its specific layers 
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="asepriteFilePath"></param>
    /// <param name="animationTagName"></param>
    /// <param name="callableAnimationName"></param>
    /// <param name="layers"></param>
    public static void LoadAsepriteAnimationWithLayers(Entity entity, string asepriteFilePath, string animationTagName, string callableAnimationName = null, params string[] layers)
    {
        SpriteAtlas sprite = entity.Scene.Content.LoadAsepriteFile(asepriteFilePath).ToSpriteAtlasFromLayers(true, 0, 0 ,0 , null, layers);

        if (callableAnimationName == null) 
            entity.GetComponent<SpriteAnimator>().AddAnimation(animationTagName, sprite.GetAnimation(animationTagName));
        else
            entity.GetComponent<SpriteAnimator>().AddAnimation(callableAnimationName, sprite.GetAnimation(animationTagName));
    }
}
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

/*public class RuleTileSeamless : IsometricRuleTile<RuleTileSeamless.Neighbor>
{
    public int siblingGroup;
    public class Neighbor : RuleTile.TilingRule.Neighbor
    {
        public const int Sibling = 3;
    }

    public override bool RuleMatch(int neighbor, TileBase tile)
    {
        RuleTileSeamless other = tile as RuleTileSeamless;
        switch (neighbor)
        {
            case Neighbor.Sibling: if (other) return other.siblingGroup == siblingGroup; break;
        }

        return base.RuleMatch(neighbor, tile);
    }
}*/

public class BaseRuleTile : RuleTile { }
public class BaseRuleTile<T> : RuleTile<T> { }
[CreateAssetMenu]
public class RuleTileSeamless : BaseRuleTile
{

    public List<TileBase> siblings = new List<TileBase>();
    public bool matchSiblingLayer;
    public int siblingLayer;

    public override bool RuleMatch(int neighbor, TileBase other)
    {
        bool isMatchCondition = neighbor == RuleTile.TilingRule.Neighbor.This;
        bool isMatch = other == this;

        if (!isMatch)
        {
            isMatch = siblings.Contains(other);
        }

        if (!isMatch && matchSiblingLayer)
        {
            if (other is RuleOverrideTile)
                other = (other as RuleOverrideTile).m_InstanceTile;

            if (other is RuleTileSeamless)
                isMatch = siblingLayer == (other as RuleTileSeamless).siblingLayer;
        }

        return isMatch == isMatchCondition;
    }
}
using System;
using System.Collections.Generic;

/// <summary>
/// Хранит данные текущего прохождения
/// Сохраняет прогресс на карте для восстановления при перезагрузке сцены
/// </summary>
public static class RunData
{
    public static int Seed=124;
    public static int CurrentDay = 0;
    public static int Tokens = 0;

    // Прогресс на карте
    public static MapProgressData MapProgress = new MapProgressData();

    public static void Reset()
    {
        Seed = new Random().Next(0, int.MaxValue);
        CurrentDay = 0;
        Tokens = 0;
        MapProgress.Reset();
    }

    public static void ResetMapOnly()
    {
        MapProgress.Reset();
    }
}

/// <summary>
/// Данные прогресса на карте
/// </summary>
[System.Serializable]
public class MapProgressData
{
    // Текущий узел (layer, position)
    public int currentNodeLayer = -1;
    public int currentNodePosition = -1;
    
    // Пройденные узлы
    public List<NodeIdentifier> completedNodes = new List<NodeIdentifier>();
    
    // Есть ли сохраненный прогресс
    public bool HasProgress => currentNodeLayer >= 0 && currentNodePosition >= 0;

    public void Reset()
    {
        currentNodeLayer = -1;
        currentNodePosition = -1;
        completedNodes.Clear();
    }

    public void SetCurrentNode(int layer, int position)
    {
        currentNodeLayer = layer;
        currentNodePosition = position;
    }

    public void AddCompletedNode(int layer, int position)
    {
        var identifier = new NodeIdentifier(layer, position);
        if (!completedNodes.Contains(identifier))
        {
            completedNodes.Add(identifier);
        }
    }

    public bool IsNodeCompleted(int layer, int position)
    {
        return completedNodes.Contains(new NodeIdentifier(layer, position));
    }
}

/// <summary>
/// Идентификатор узла на карте
/// </summary>
[Serializable]
public struct NodeIdentifier : IEquatable<NodeIdentifier>
{
    public int layer;
    public int position;

    public NodeIdentifier(int layer, int position)
    {
        this.layer = layer;
        this.position = position;
    }

    public bool Equals(NodeIdentifier other)
    {
        return layer == other.layer && position == other.position;
    }

    public override bool Equals(object obj)
    {
        return obj is NodeIdentifier other && Equals(other);
    }

    public override int GetHashCode()
    {
        return layer * 1000 + position;
    }

    public override string ToString()
    {
        return $"L{layer}P{position}";
    }
}
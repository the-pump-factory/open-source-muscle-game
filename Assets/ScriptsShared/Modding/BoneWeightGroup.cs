using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.Linq;

public class BoneWeightGroup
{

    protected Dictionary<int, float> rawWeights = new Dictionary<int, float>();

    public void Add(BoneWeight1 weight, float multiplier = 1) 
    {

        rawWeights.TryGetValue(weight.boneIndex, out float weightF);

        weightF += weight.weight * multiplier;

        rawWeights[weight.boneIndex] = weightF;
    
    }

    public List<BoneWeight1> Extract(int maxCount = -1)
    {

        List<BoneWeight1> temp = new List<BoneWeight1>();

        foreach (var element in rawWeights) temp.Add(new BoneWeight1() { boneIndex = element.Key, weight = element.Value });

        temp = temp.OrderByDescending(i => i.weight).ToList();

        if (maxCount > 0 && maxCount < temp.Count) temp.RemoveRange(maxCount, temp.Count - maxCount);

        float total = 0;

        for (int a = 0; a < temp.Count; a++) total += temp[a].weight;

        if (total > 0) for (int a = 0; a < temp.Count; a++) temp[a] = new BoneWeight1() { boneIndex = temp[a].boneIndex, weight = temp[a].weight / total };
           
        return temp;

    }

    public BoneWeight ExtractDefault()
    {

        List<BoneWeight1> temp = Extract(4);

        BoneWeight bw = new BoneWeight();

        if (temp.Count > 0)
        {

            bw.boneIndex0 = temp[0].boneIndex;
            bw.weight0 = temp[0].weight;

        }

        if (temp.Count > 1)
        {

            bw.boneIndex1 = temp[1].boneIndex;
            bw.weight1 = temp[1].weight;

        }

        if (temp.Count > 2)
        {

            bw.boneIndex2 = temp[2].boneIndex;
            bw.weight2 = temp[2].weight;

        }

        if (temp.Count > 3)
        {

            bw.boneIndex3 = temp[3].boneIndex;
            bw.weight3 = temp[3].weight;

        }

        float total = bw.weight0 + bw.weight1 + bw.weight2 + bw.weight3;

        if (total > 0)
        {

            bw.weight0 = bw.weight0 / total;
            bw.weight1 = bw.weight1 / total;
            bw.weight2 = bw.weight2 / total;
            bw.weight3 = bw.weight3 / total;

        }

        return bw;

    }

}

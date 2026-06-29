using System;

namespace ExamAI.Grading.Worker.Services;

public interface IStringSimilarityService
{
    double ComputeSimilarity(string source, string target);
}

public class StringSimilarityService : IStringSimilarityService
{
    public double ComputeSimilarity(string source, string target)
    {
        if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(target)) 
            return 0.0;
            
        if (source.Trim() == target.Trim()) 
            return 1.0;

        int sourceLen = source.Length;
        int targetLen = target.Length;
        int[,] distanceMatrix = new int[sourceLen + 1, targetLen + 1];

        // אתחול המטריצה
        for (int i = 0; i <= sourceLen; distanceMatrix[i, 0] = i++) { }
        for (int j = 0; j <= targetLen; distanceMatrix[0, j] = j++) { }

        // אלגוריתם לוינשטיין (Levenshtein)
        for (int i = 1; i <= sourceLen; i++)
        {
            for (int j = 1; j <= targetLen; j++)
            {
                int cost = (target[j - 1] == source[i - 1]) ? 0 : 1;
                distanceMatrix[i, j] = Math.Min(
                    Math.Min(distanceMatrix[i - 1, j] + 1, distanceMatrix[i, j - 1] + 1),
                    distanceMatrix[i - 1, j - 1] + cost);
            }
        }

        // חישוב אחוז הדמיון מתוך המרחק
        int maxLen = Math.Max(sourceLen, targetLen);
        int distance = distanceMatrix[sourceLen, targetLen];
        
        return 1.0 - ((double)distance / maxLen);
    }
}
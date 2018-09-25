using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RdN
{
    /// <summary>
    /// type de réseau de neurones
    /// </summary>
    public enum RdN_Type
    {
        /// <summary>
        /// réseau de neurone classifieur
        /// </summary>
        CLASSIFIEUR,
        /// <summary>
        /// type de réseau inconnu ou général
        /// </summary>
        UNKNOW
    }

    static public class Builder
    { 
        /// <summary>
        /// Préfabrique un réseau de neurone
        /// </summary>
        /// <param name="nbrEntrees">nbr d'entrées du réseau de neurones</param>
        /// <param name="nbrSorties">nbr de sortie du RdN</param>
        /// <param name="type">type de RdN</param>
        /// <returns>RdN préfabriqué</returns>
        static public Reseau CreerReseau(int nbrEntrees, int nbrSorties, RdN_Type type)
        {
            int[] couches;
            switch (type)
            {
                case RdN_Type.CLASSIFIEUR:
                case RdN_Type.UNKNOW:
                default:
                    couches = new int[Math.Max(3, (int)Math.Round(
                                                        Math.Log(
                                                             Math.Abs(nbrEntrees - nbrSorties))))];
                    double b = nbrEntrees / 2;
                    double a = (nbrSorties - b) / couches.Length;
                    
                    for (int i = 0; i < couches.Length-1; i++)
                    {
                        couches[i] = Math.Max(nbrSorties, (int)(a * i + b));   
                    }
                    couches[couches.Length-1] = nbrSorties; //pour etre sur que la dernière couche est le nombre de sortie qu'il faut
                    break;
            }
            return new Reseau(couches, nbrEntrees);
        }
    }
}

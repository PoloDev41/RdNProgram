using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RdN
{
    static public class Apprentissage
    {
        /// <summary>
        /// fait apprendre au réseau de neurone par rétropropagation
        /// </summary>
        /// <param name="reseau">réseau à faire apprendre</param>
        /// <param name="entrees">échantillon d'entrées</param>
        /// <param name="sortiesAttendues">échantillon de sortie attendues</param>
        static public void LearnRetropropagation(ref Reseau reseau, double[] entrees, double[] sortiesAttendues)
        {
            reseau.Calcul(entrees);
            reseau.Erreur = new double[reseau.Sortie.Length];
            double tmpErreurQuadra = 0f;
            for (int i = 0; i < reseau.Sortie.Length; i++)
            {
                reseau.Erreur[i] = sortiesAttendues[i] - reseau.Sortie[i];
                tmpErreurQuadra += Math.Pow(reseau.Erreur[i], 2);
            }
            reseau.ErreurQuadratique = tmpErreurQuadra / 2f;
            reseau.Apprentissage(entrees);
        }

        /// <summary>
        /// fait apprendre au réseau de neurone par rétropropagation
        /// </summary>
        /// <param name="reseau">réseau à faire apprendre</param>
        /// <param name="nbrEssaiMax">nombre d'essai max avant d'arreter</param>
        /// <param name="erreurMax">erreur max à atteindre</param>
        /// <param name="entrees">échantillon d'entrées</param>
        /// <param name="sortiesAttendues">échantillon de sortie attendues</param>
        static public void LearnRetropropagation(ref Reseau reseau, int nbrEssaiMax, double erreurMax, double[][] entrees, double[][] sortiesAttendues)
        {
            int essai = 0;
            double erreur;
            double coeff = 0.5f / (1-nbrEssaiMax);
            double b = 0.9f - coeff;
            do
            {
                essai++;
                if (nbrEssaiMax != 1)
                    reseau.CoeffApprentissage = coeff * essai + b;
                else
                    reseau.CoeffApprentissage = .75f;
                erreur = 0f;

                for (int i = 0; i < entrees.Length; i++)
                {
                    Apprentissage.LearnRetropropagation(ref reseau, entrees[i], sortiesAttendues[i]);
                    erreur += reseau.ErreurQuadratique;
                }
                erreur /= entrees.Length;
                reseau.ErreurQuadratiqueMoyenne = erreur;

                if (erreur < erreurMax)
                    return; //erreur atteinte

            } while (essai < nbrEssaiMax);

            reseau.CoeffApprentissage = .75f;
        }

        /// <summary>
        /// Crée des hybride du réseau original pour l'auto construction
        /// </summary>
        /// <param name="Original">réseau servant de modèle</param>
        /// <param name="Suppression">true: on cree des individus avec des neurones en moins</param>
        /// <returns>les individus</returns>
        static private List<Reseau> CreationIndividu(ref Reseau Original, bool Suppression)
        {
            int num = 0;
            List<Reseau> listReseau = new List<Reseau>();

            listReseau.Add(Original.Clone());
            listReseau[num++].AjoutCouche(Neurone.RandomFctActivation());//test avec une couche en plus

            listReseau.Add(Original.Clone());
            listReseau[num++].AjoutNeurone(); //test avec un neurone en plus

            //on change une fonction d'activation sur une couche complète
            listReseau.Add(Original.Clone());
            listReseau[num++].ChangerFonctionActivation(Neurone.RandomFctActivation());

            //on change une fonction d'activation sur un neurone
            listReseau.Add(Original.Clone());
            listReseau[num++].ChangerFonctionActivationNeurone(Neurone.RandomFctActivation());

            if (Suppression)
            {
                num++;
                int[] nbrCouches = new int[Original.ReseauNeurones.Length];
                for (int i = 0; i < nbrCouches.Length; i++)
                {
                    nbrCouches[i] = Original.ReseauNeurones[i].CoucheNeurones.Length;
                }
                listReseau.Add(new Reseau(nbrCouches, Original.ReseauNeurones[0].CoucheNeurones[0].Poids.Length));

                listReseau.Add(Original.Clone());
                listReseau[num++].SupprimerCouche(); //test avec une couche aléatoire en moins

                listReseau.Add(Original.Clone());
                listReseau[num++].SupprimerNeuroneNegligeable(); //test avec un neurone négligeable en moins

                listReseau.Add(Original.Clone());
                listReseau[num++].SupprimerPetitNeurone(); //test avec le neurone le moins pondérant dans le réseau
            }
            return listReseau;
        }


        /// <summary>
        /// fait apprendre le réseau neuronal par rétropropagation et par auto-construction
        /// </summary>
        /// <param name="reseau">réseau à faire apprendre (attention, le nombre de neurones peut changer)</param>
        /// <param name="erreurMax">erreur quadratique maximum à atteindre pour s'arreter</param>
        /// <param name="nbrEssaiMax">nombre d'essai maximum si erreur non atteinte</param>
        /// <param name="entrees">jeux d'échantillons d'entrées</param>
        /// <param name="sortieAttendues">jeux d'échantillons de sortie attendues</param>
        /// <param name="suppression">test avec des individus avec des neurones en moins</param>
        static public void LearnConstruction(ref Reseau reseau, double erreurMax, int nbrEssaiMax,
                                            double[][] entrees, double[][] sortieAttendues, bool suppression)
        {
            List<Reseau> listReseau = new List<Reseau>();
            bool MaintienCoeffInertie = false;
            //on regarde l'erreur, si c'est pas bon: auto-construction
            if (reseau.ErreurQuadratiqueMoyenne > erreurMax)
            {
                //init des variables de la boucle
                listReseau.Clear();

                //création de la liste des réseaux à tester
                listReseau.Add(reseau);
                listReseau.AddRange(CreationIndividu(ref reseau, suppression));

                //on lance en parallèle l'apprentissage
                Parallel.ForEach(listReseau, item =>
                                    LearnRetropropagation(ref item, nbrEssaiMax, erreurMax,
                                                            entrees, sortieAttendues));
                //on trie dans l'ordre du meilleur
                listReseau.Sort(SortReseau);
                reseau = listReseau[0]; //le réseau devient le meilleur des réseaux

                if (MaintienCoeffInertie)
                {
                    reseau.CoeffInertie = Math.Max(reseau.CoeffInertie - 0.05f, 0.1f);
                    if (reseau.CoeffInertie < 0.1)
                        MaintienCoeffInertie = false;
                }
                else
                {
                    reseau.CoeffInertie = Math.Min(reseau.CoeffInertie + 0.05f, 0.5f);
                    if (reseau.CoeffInertie > 0.45)
                        MaintienCoeffInertie = true;
                }
            }
            reseau.CoeffInertie = .1f;
        }

        /// <summary>
        /// trie le réseau de neurone en fonction de l'erreur quadratique
        /// </summary>
        /// <param name="x">x</param>
        /// <param name="y">y</param>
        /// <returns>le meilleur</returns>
        static private int SortReseau(Reseau x, Reseau y)
        {
            if (x.ErreurQuadratiqueMoyenne > y.ErreurQuadratiqueMoyenne)
                return 1;
            else if (x.ErreurQuadratiqueMoyenne < y.ErreurQuadratiqueMoyenne)
                return -1;
            else return 0;
        }
    }
}

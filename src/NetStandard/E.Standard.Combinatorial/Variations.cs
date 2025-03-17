using System;

namespace E.Standard.Combinatorial;

/// <summary>
/// Generates all the variations but NOT in lexicographical oreder
/// </summary>
public class Variations : CombinatorialBase
{

    bool m_bPermutateNow = true;
    protected int[] m_arrayIndecesPermutation;
    protected int[] m_arrayIndecesCombination;

    public Variations(Array arrayObjects, int nKlass) :
        base(arrayObjects, nKlass)
    {
        m_arrayIndecesPermutation = new int[nKlass];
        m_arrayIndecesCombination = new int[nKlass];
    }

    public Variations(System.Collections.IList listObjects, int nKlass) :
        base(listObjects, nKlass)
    {
        m_arrayIndecesPermutation = new int[nKlass];
        m_arrayIndecesCombination = new int[nKlass];
    }

    public Variations(System.Collections.IEnumerator enumeratorObjects, int nKlass) :
        base(enumeratorObjects, nKlass)
    {
        m_arrayIndecesPermutation = new int[nKlass];
        m_arrayIndecesCombination = new int[nKlass];
    }

    private void FirstIndecesPermutation()
    {

        // Reinitialize the permutation index.
        for (int i = 0; i < m_arrayIndecesPermutation.Length; i++)
        {
            m_arrayIndecesPermutation[i] = i;
        }
    }

    override protected int[] FirstIndeces(bool bReturnDublicate)
    {

        // Do some extra initializing.
        FirstIndecesPermutation();

        // Reinitialize the combination index.
        for (int i = 0; i < m_arrayIndecesCombination.Length; i++)
        {
            m_arrayIndecesCombination[i] = i;
        }

        // Call the father method, because it is needed.
        return base.FirstIndeces(bReturnDublicate);
    }


    /*virtual*/
    override protected int[] NextIndeces(bool bReturnDublicate)
    {

        if (!m_bInitialized)
        {
            return FirstIndeces(false);
        }


        if (m_bPermutateNow)
        {
            // Generate a permutation of the current variation.
            int[] array = NextIndecesPermutation(bReturnDublicate);

            if (array.Length != 0)
            {
                return array;
            }

            // If we got here we cannot generate more permutations from
            // the current combination so we advance to the next combination
            // by allowing the CODE FLOW to execute the code bellow.
            m_bPermutateNow = false;
        }

        // Find the first item that has not reached its maximum value.
        int nIndex = m_nKlass;
        for (int i = m_arrayIndecesCombination.Length - 1; i >= 0; i--)
        {
            if (m_arrayIndecesCombination[i] < m_nMaxIndex - (m_nKlass - 1 - i))
            {
                nIndex = i;
                break;
            }
        }

        // No more combinations to be generated. Every item has reached its
        // maximum value.
        if (nIndex == m_nKlass)
        {
            return new int[0];
        }

        // Genereate the next combination in lexographical order.
        m_arrayIndecesCombination[nIndex]++;
        for (int i = nIndex + 1; i < m_arrayIndecesCombination.Length; i++)
        {
            m_arrayIndecesCombination[i] = m_arrayIndecesCombination[i - 1] + 1;
        }

        // A new cobination has been generated. The next time we must
        // permutate it.
        m_bPermutateNow = true;
        FirstIndecesPermutation();

        // Absolutely needed copy.
        Array.Copy(m_arrayIndecesCombination, m_arrayIndeces, m_nKlass);

        if (bReturnDublicate)
        {
            // Copy not to allow modification of m_arrayIndeces.
            Array.Copy(m_arrayIndeces, m_arrayIndecesDummy, m_nKlass);
            return m_arrayIndecesDummy;
        }
        else
        {
            return m_arrayIndeces;
        }
    } // End of NextIndeces

    protected int[] NextIndecesPermutation(bool bReturnDublicate)
    {

        int nIndexLast = m_arrayIndecesPermutation.Length - 1;

        // Find the first item so that a[i] < a[i+1];
        int nIndexI = m_nKlass;
        for (int i = nIndexLast - 1; i >= 0; i--)
        {
            if (m_arrayIndecesPermutation[i] < m_arrayIndecesPermutation[i + 1])
            {
                nIndexI = i;
                break;
            }
        }

        // Find the smallest a[j] , so that j > i & a[j] > a[i]
        int nIndexJ = nIndexLast;
        for (int j = nIndexJ; j > nIndexI; j--)
        {
            if (m_arrayIndecesPermutation[j] > m_arrayIndecesPermutation[nIndexI])
            {
                nIndexJ = j;
                break;
            }
        }

        // No more permutations to be generated. 
        if (nIndexI == m_nKlass)
        {
            return new int[0];
        }

        // Exchange the a[i] and the last item (a[n]).
        int nTmp = m_arrayIndecesPermutation[nIndexI];
        m_arrayIndecesPermutation[nIndexI] = m_arrayIndecesPermutation[nIndexJ];
        m_arrayIndecesPermutation[nIndexJ] = nTmp;

        // Reverse the items from a[i+1] till a[n];
        int i0 = nIndexI + 1;
        int j0 = nIndexLast;
        while (i0 < j0)
        {
            nTmp = m_arrayIndecesPermutation[i0];
            m_arrayIndecesPermutation[i0] = m_arrayIndecesPermutation[j0];
            m_arrayIndecesPermutation[j0] = nTmp;
            i0++;
            j0--;
        }

        for (int i1 = 0; i1 < m_nKlass; i1++)
        {
            int nIndex = m_arrayIndecesPermutation[i1];
            m_arrayIndeces[i1] = m_arrayIndecesCombination[nIndex];
        }

        // The RETURN section.
        if (bReturnDublicate)
        {
            // Copy not to allow modification of m_arrayIndeces.
            Array.Copy(m_arrayIndeces, m_arrayIndecesDummy, m_nKlass);
            return m_arrayIndecesDummy;
        }
        else
        {
            return m_arrayIndeces;
        }

    } // End of NextIndecesPermutation

}

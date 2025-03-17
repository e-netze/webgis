using System;

namespace E.Standard.Combinatorial;

/// <summary>
/// Generates all the permutations in lexicographical order
/// </summary>
public class Permutations : Combinatorial.CombinatorialBase
{
    public Permutations(Array arrayObjects) : base(arrayObjects, arrayObjects.Length)
    {
    }

    public Permutations(System.Collections.IList listObjects) : base(listObjects, listObjects.Count)
    {
    }

    public Permutations(System.Collections.IEnumerator enumeratorObjects) : base(enumeratorObjects, 1)
    {

        // In permutations the class is the same as the number of elements 
        // in the object collection.
        m_nKlass = m_nMaxIndex + 1;

        // We need to reinitialize these objects because in the constuctor call
        // of the base class we pass "1" as "nKlass" argument which is not proper
        // in most cases.
        m_arrayIndeces = new int[m_nKlass];
        m_arrayIndecesDummy = new int[m_nKlass];
        m_arrayCurrentObj = new Object[m_nKlass];
    }

    /*virtual*/
    override protected int[] NextIndeces(bool bReturnDublicate)
    {

        if (!m_bInitialized)
        {
            return FirstIndeces(false);
        }

        int nIndexLast = m_arrayIndeces.Length - 1;

        // Find the first item so that a[i] < a[i+1];
        int nIndexI = m_nKlass;
        for (int i = nIndexLast - 1; i >= 0; i--)
        {
            if (m_arrayIndeces[i] < m_arrayIndeces[i + 1])
            {
                nIndexI = i;
                break;
            }
        }

        // Find the smallest a[j] , so that j > i & a[j] > a[i]
        int nIndexJ = nIndexLast;
        for (int j = nIndexJ; j > nIndexI; j--)
        {
            if (m_arrayIndeces[j] > m_arrayIndeces[nIndexI])
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
        int nTmp = m_arrayIndeces[nIndexI];
        m_arrayIndeces[nIndexI] = m_arrayIndeces[nIndexJ];
        m_arrayIndeces[nIndexJ] = nTmp;

        // Reverse the items from a[i+1] till a[n];
        int i0 = nIndexI + 1;
        int j0 = nIndexLast;
        while (i0 < j0)
        {
            nTmp = m_arrayIndeces[i0];
            m_arrayIndeces[i0] = m_arrayIndeces[j0];
            m_arrayIndeces[j0] = nTmp;
            i0++;
            j0--;
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

    } // End of NextIndeces

}

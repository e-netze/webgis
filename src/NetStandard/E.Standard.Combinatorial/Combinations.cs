using System;

namespace E.Standard.Combinatorial;

/// <summary>
/// Generates cobination without dublications in lexicographical order
/// </summary>
public class Combinations : CombinatorialBase
{
    public Combinations(Array arrayObjects, int nKlass) :
        base(arrayObjects, nKlass)
    {
    }

    public Combinations(System.Collections.IList listObjects, int nKlass) :
        base(listObjects, nKlass)
    {
    }

    public Combinations(System.Collections.IEnumerator enumeratorObjects, int nKlass) :
        base(enumeratorObjects, nKlass)
    {
    }

    /*virtual*/
    override protected int[] NextIndeces(bool bReturnDublicate)
    {

        if (!m_bInitialized)
        {
            return FirstIndeces(false);
        }

        // Find the first item that has not reached its maximum value.
        int nIndex = m_nKlass;
        for (int i = m_arrayIndeces.Length - 1; i >= 0; i--)
        {
            if (m_arrayIndeces[i] < m_nMaxIndex - (m_nKlass - 1 - i))
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
        m_arrayIndeces[nIndex]++;
        for (int i = nIndex + 1; i < m_arrayIndeces.Length; i++)
        {
            m_arrayIndeces[i] = m_arrayIndeces[i - 1] + 1;
        }

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

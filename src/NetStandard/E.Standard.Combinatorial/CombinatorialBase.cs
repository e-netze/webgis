using System;
using System.Collections;


namespace E.Standard.Combinatorial;

/// <summary>
/// 
/// </summary>
abstract public class CombinatorialBase : IEnumerator
{
    // This is a local copy of the items that need to be supplied 
    // in various combinatorial forms.
    // !! NOTE !! : This must be a shallow copy of the original list of items.
    //				or it must be a deep one ???
    protected Array m_arrayObj;

    protected int m_nKlass;
    protected int m_nMaxIndex;

    // Because we use m_arrayIndeces internally to store the current combination
    // we need the DUMMY array to be returned to the user so that he cannot modify
    // the internal state of the COMBINATION which will be possible if 
    // we return direcly m_arrayIndeces. This is one of the weaknesses of the 
    // C# & CLI -> There is no const-nes over arrays in the sence of C++, too bad :-(
    protected int[] m_arrayIndeces;
    protected int[] m_arrayIndecesDummy;

    // Hold the items that build up the current combination.
    protected Array m_arrayCurrentObj;

    protected bool m_bInitialized = false;

    // This is the GENERIC Type constructor.
    protected CombinatorialBase(Array arrayObjects, int nKlass)
    {
        // Check the validity of the arguments
        DoArgumentCheck(arrayObjects.Length, nKlass);

        m_nKlass = nKlass;

        // Always takes the ZERO (FIRST) dimension of the array. There is no
        // problem in manipulation multidimensional arrays.
        m_nMaxIndex = arrayObjects.GetLength(0) - 1;

        m_arrayIndeces = new int[m_nKlass];
        m_arrayIndecesDummy = new int[m_nKlass];

        m_arrayCurrentObj = new Object[m_nKlass];
        //			m_arrayCurrentCombination = Array.CreateInstance( 
        //				arrayObjects.GetValue(0).GetType(), m_nKlass);

        // Make a shallow copy of the source array.
        //			m_arrayObj = Array.CreateInstance( arrayObjects.GetValue(0).GetType(), arrayObjects.Length);
        m_arrayObj = Array.CreateInstance(Type.GetType("System.Object"), arrayObjects.Length);
        Array.Copy(arrayObjects, m_arrayObj, arrayObjects.Length);
    }


    // Handles the lists of elements.
    protected CombinatorialBase(IList listObjects, int nKlass)
    {

        // Check the validity of the arguments
        DoArgumentCheck(listObjects.Count, nKlass);

        m_nKlass = nKlass;

        // Always takes the ZERO (FIRST) dimension of the array. There is no
        // problem in manipulation multidimensional arrays.
        m_nMaxIndex = listObjects.Count - 1;

        m_arrayIndeces = new int[m_nKlass];
        m_arrayIndecesDummy = new int[m_nKlass];

        m_arrayCurrentObj = new Object[m_nKlass];

        // Make a shallow copy of the source array.
        //			m_arrayObj = Array.CreateInstance(listObjects[0].GetType(), listObjects.Count);
        m_arrayObj = Array.CreateInstance(Type.GetType("System.Object"), listObjects.Count);
        listObjects.CopyTo(m_arrayObj, 0);
    }


    // Handles the lists of elements.
    protected CombinatorialBase(IEnumerator enumeratorObjects, int nKlass)
    {

        m_nKlass = nKlass;

        // Because when an enumerator is used we don't know the exact number of items,
        // in order to create an array we must first go through all the objects once.
        int nEnumCount = 0;
        enumeratorObjects.Reset();
        while (enumeratorObjects.MoveNext())
        {
            nEnumCount++;
        }

        // Check the validity of the arguments
        DoArgumentCheck(nEnumCount, nKlass);

        m_nMaxIndex = nEnumCount - 1;

        m_arrayIndeces = new int[m_nKlass];
        m_arrayIndecesDummy = new int[m_nKlass];

        m_arrayCurrentObj = new Object[m_nKlass];

        // Make a shallow copy of the source enumerator.
        m_arrayObj = Array.CreateInstance(Type.GetType("System.Object"), nEnumCount);
        int i = 0;
        enumeratorObjects.Reset();
        while (enumeratorObjects.MoveNext())
        {
            Object obj = enumeratorObjects.Current;
            m_arrayObj.SetValue(obj, i);
            i++;
        }
    }

    virtual protected void DoArgumentCheck(int nItems, int nKlass)
    {

        if (nKlass <= 0)
        {
            throw new ArgumentOutOfRangeException("nKlass", nKlass,
            "Second parameter (nKlass) to CombinatorialBase constructor must be > 0");
        }

        if (nItems < nKlass)
        {
            throw new ArgumentOutOfRangeException("nKlass", nKlass,
                "Less than needed objects supplied. Second " +
                "parameter of CombinatorialBase cannot be greater that the number " +
                "of objects");
        }
    }


    public Object Current
    {
        get { return CurrentItems(); }
    }

    public bool MoveNext()
    {

        int[] indeces = NextIndeces(false);

        if (indeces.Length > 0)
        {
            return true;
        }
        else
        {
            return false;
        }
    }


    public void Reset()
    {
        m_bInitialized = false;
    }


    protected int[] FirstIndeces()
    {
        return FirstIndeces(true);
    }

    // This one is made virtual. If you need a different initializing
    // sequence than the default {0, 1, 2, ..., N} then override it.
    virtual protected int[] FirstIndeces(bool bReturnDublicate)
    {
        for (int i = 0; i < m_arrayIndeces.Length; i++)
        {
            m_arrayIndeces[i] = i;
        }

        m_bInitialized = true;

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
    } // End of FirstIndeces

    protected int[] NextIndeces()
    {
        return NextIndeces(true);
    }

    abstract protected int[] NextIndeces(bool bReturnDublicate);

    protected Array NextItems()
    {

        // Generate the indeces of the elements that are going to
        // take part in the next combination.
        int[] res = NextIndeces(false);

        if (res == null)
        {
            return null;
        }

        for (int j = 0; j < m_arrayIndeces.Length; j++)
        {
            int nIndex = m_arrayIndeces[j];
            m_arrayCurrentObj.SetValue(m_arrayObj.GetValue(nIndex), j);
        }

        return m_arrayCurrentObj;
    }


    public int[] CurrentIndeces
    {

        get
        {
            if (!m_bInitialized)
            {
                throw new InvalidOperationException("CombinatorialBase collection must be Reset() before usage");
            }

            // Copy not to allow modification of m_arrayIndeces.
            Array.Copy(m_arrayIndeces, m_arrayIndecesDummy, m_nKlass);
            return m_arrayIndecesDummy;
        }
    }


    protected Array CurrentItems()
    {

        if (!m_bInitialized)
        {
            throw new InvalidOperationException("CombinatorialBase collection must be Reset() before usage");
        }

        // Fill the return array properly.
        for (int j = 0; j < m_arrayIndeces.Length; j++)
        {
            int nIndex = m_arrayIndeces[j];
            m_arrayCurrentObj.SetValue(m_arrayObj.GetValue(nIndex), j);
        }

        // And return it.
        return m_arrayCurrentObj;
    }

}

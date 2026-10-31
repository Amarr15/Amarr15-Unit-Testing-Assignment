public class Solution
{
    public int MinEatingSpeed(int[] piles, int h)
    {
        int low = 1;
        int high = 0;

        foreach (int pile in piles)
        {
            if (pile > high)
            {
                high = pile;
            }
        }

        while (low < high)
        {
            int mid = low + (high - low) / 2;

            int hours = 0;

            foreach (int pile in piles)
            {
                hours += (pile + mid - 1) / mid;
            }

            if (hours <= h)
            {
                high = mid;
            }
            else
            {
                low = mid + 1;
            }
        }

        return low;
    }
}
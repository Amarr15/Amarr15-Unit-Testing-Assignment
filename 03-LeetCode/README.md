# LeetCode - Binary Search

- **Public Profile**: [Amar31](https://leetcode.com/u/Amar31/)

## 1. 875. Koko Eating Bananas

- **Submission Link**: [Accepted Submission](https://leetcode.com/problems/koko-eating-bananas/submissions/2186378107)
- **Time Complexity**: O(n log m)
- **Space Complexity**: O(1)

### Range Explanation
- `low = 1`: Minimum eating speed.
- `high = max(piles)`: Maximum eating speed needed.
- If the required hours are within `h`, we try a smaller speed.
- Otherwise, we increase the speed.
- The loop ends when `low == high`, which is the minimum valid speed.

### Example 1 Trace

Input: `piles = [3, 6, 7, 11]`, `h = 8`

| Low | Mid | High | Hours | Decision |
|---:|---:|---:|---:|---|
| 1 | 6 | 11 | 6 | Search left |
| 1 | 3 | 6 | 10 | Search right |
| 4 | 5 | 6 | 8 | Search left |
| 4 | 4 | 5 | 8 | Search left |

**Answer: 4**

---

## 2. 33. Search in Rotated Sorted Array

- **Submission Link**: [Accepted Submission](https://leetcode.com/problems/search-in-rotated-sorted-array/submissions/2186379372)
- **Time Complexity**: O(log n)
- **Space Complexity**: O(1)

### Range Explanation
- `low = 0`: First index.
- `high = nums.Length - 1`: Last index.
- At least one half is always sorted.
- We check which half is sorted and whether the target belongs to it.
- We discard the other half and continue until the target is found or `low > high`.

### Example 1 Trace

Input: `nums = [4, 5, 6, 7, 0, 1, 2]`, `target = 0`

| Low | Mid | High | nums[mid] | Decision |
|---:|---:|---:|---:|---|
| 0 | 3 | 6 | 7 | Search right |
| 4 | 5 | 6 | 1 | Search left |
| 4 | 4 | 4 | 0 | Found |

**Answer: 4**
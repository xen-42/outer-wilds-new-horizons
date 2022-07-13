using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

using Logger = NewHorizons.Utility.Logger;

namespace NewHorizons.Components
{
    public class NHMultiStateQuantumObject : MultiStateQuantumObject
    {
        
	    public override bool ChangeQuantumState(bool skipInstantVisibilityCheck)
	    {
		    for (int i = 0; i < _prerequisiteObjects.Length; i++)
		    {
			    if (!_prerequisiteObjects[i].HasCollapsed())
			    {
				    return false;
			    }
		    }
		    int stateIndex = _stateIndex;
		    if (_stateIndex == -1 && _initialState != -1)
		    {
			    _stateIndex = _initialState;
		    }
		    else if (_sequential)
		    {
			    _stateIndex = (_reverse ? (_stateIndex - 1) : (_stateIndex + 1));
			    if (_loop)
			    {
				    if (_stateIndex < 0)
				    {
					    _stateIndex = _states.Length - 1;
				    }
				    else if (_stateIndex > _states.Length - 1)
				    {
					    _stateIndex = 0;
				    }
			    }
			    else
			    {
				    _stateIndex = Mathf.Clamp(_stateIndex, 0, _states.Length - 1);
			    }
		    }
		    else
		    {    

                // TODO: perform this roll for number of states, each time adding the selected state to the end of a list and removing it from the source list
                // this gets us a randomly ordered list that respects states' probability
                // then we can sequentially attempt collapsing to them, checking at each state whether the new state is invalid due to the player being able to see it, according to this:
                //
                // if (!((!IsPlayerEntangled()) ? (CheckIllumination() ? CheckVisibilityInstantly() : CheckPointInside(Locator.GetPlayerCamera().transform.position)) : CheckIllumination()))
			    // {
				//     return true; // this is a valid state
			    // }
                //

                List<int> indices = new List<int>();
                for (var i = 0; i < _states.Length; i++) if (i != stateIndex) indices.Add(i);
                
                var previousIndex = stateIndex;

                do
                {
                    previousIndex = _stateIndex;
                    _stateIndex = RollState(stateIndex, indices);
                    if (previousIndex >= 0 && previousIndex < _states.Length) _states[previousIndex].SetVisible(visible: false);
                    _states[_stateIndex].SetVisible(visible: true);
        
                    Logger.LogVerbose($"MultiStateQuantumObject - Trying to change state {_stateIndex}");

                    indices.Remove(_stateIndex);
                } while (!CurrentStateIsValid() && indices.Count > 0);
		    }

            var stateIndexIsValid = stateIndex >= 0 && stateIndex < _states.Length;
		    if (stateIndexIsValid) _states[stateIndex].SetVisible(visible: false);

		    _states[_stateIndex].SetVisible(visible: true);
            if (!CurrentStateIsValid() && stateIndexIsValid)
            {
		        _states[_stateIndex].SetVisible(visible: false);
		        _states[stateIndex] .SetVisible(visible: true);
		        _stateIndex = stateIndex;
                return false;
            }

		    if (_sequential && !_loop && _stateIndex == _states.Length - 1)
		    {
			    SetActivation(active: false);
		    }
		    return true;
	    }

        public bool CurrentStateIsValid()
        {
            var isPlayerEntangled = IsPlayerEntangled();
            var illumination = CheckIllumination();
            var visibility = CheckVisibilityInstantly();
            var playerInside = CheckPointInside(Locator.GetPlayerCamera().transform.position);

            var isVisible = 
                isPlayerEntangled
                ? illumination
                : (
                    illumination 
                    ? visibility
                    : playerInside
                );

            return !isVisible;
        }

        public int RollState(int excludeIndex, List<int> indices)
        { 
            var stateIndex = excludeIndex;

            // this function constructs a sort of segmented range:
            // 0                   3     5  6
            // +-------------------+-----+--+
            // | state 1           | s2  |s3|
            // +-------------------+-----+--+
            //
            // num is the max value of this range (min is always 0)
            // num2 is a random point on this range
            //
            // num3 and num4 track the bounds of the current segment being considered
            // num3 is the min value, num4 is the max. for example, if num3=5 then num4=6
            //
            // the second for looop uses num3 and num4 to figure out which segment num2 landed in

			int num = 0;
			foreach (int j in indices)
			{
				if (j != stateIndex)
				{
					_probabilities[j] = _states[j].GetProbability();
					num += _probabilities[j];
				}
			}
			int num2 = UnityEngine.Random.Range(0, num);
			int num3 = 0;
			int num4 = 0;
			foreach (int k in indices)
			{
				if (k != stateIndex)
				{
					num3 = num4;
					num4 += _probabilities[k];
					if (_probabilities[k] > 0 && num2 >= num3 && num2 < num4)
					{
						return k;
					}
				}
			}

            return indices[indices.Count-1];
        }
    }
}

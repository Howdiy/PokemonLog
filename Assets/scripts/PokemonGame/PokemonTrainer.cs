using UnityEngine;

/// <summary>
/// @ 포켓몬 트레이너가 보유한 포켓몬 팀을 관리하는 클래스
/// @ 선택 씬에서의 선발 저장과 배틀 중 활성 슬롯 추적을 담당한다.
/// </summary>
public class PokemonTrainer
{
    public string name;

    private readonly Pokemon[] _team;
    private int _activeIndex = -1;
    public int TeamSize
    {
        get
        {
            return _team.Length;
        }
    }

    public PokemonTrainer(string trainerName, int maxTeamSize = 3)
    {
        name = trainerName;
        if (maxTeamSize <= 0)
        {
            maxTeamSize = 1;
        }

        _team = new Pokemon[maxTeamSize];
    }
    public Pokemon[] Team
    {
        get
        {
            return _team;
        }
    }

    public int ActiveIndex
    {
        get => _activeIndex;
        private set => _activeIndex = Mathf.Clamp(value, -1, TeamSize - 1);
    }

    public Pokemon ActivePokemon
    {
        get
        {
            if (ActiveIndex < 0 || ActiveIndex >= TeamSize)
            {
                return null;
            }

            return _team[ActiveIndex];
        }
    }

    public void ClearTeam()
    {
        for (int i = 0; i < _team.Length; i++)
        {
            _team[i] = null;
        }

        ActiveIndex = -1;
    }

    public int FirstEmptySlot()
    {
        for (int i = 0; i < _team.Length; i++)
        {
            if (_team[i] == null)
            {
                return i;
            }
        }

        return -1;
    }

    public int CountFilled()
    {
        int count = 0;
        for (int i = 0; i < _team.Length; i++)
        {
            if (_team[i] != null)
            {
                count += 1;
            }
        }

        return count;
    }

    public bool TryAddPokemon(Pokemon pokemon, out int slotIndex)
    {
        slotIndex = FirstEmptySlot();
        if (slotIndex < 0)
        {
            return false;
        }

        _team[slotIndex] = pokemon;
        if (slotIndex == 0)
        {
            ActiveIndex = 0;
        }

        EnsureActivePokemonIsValid();
        return true;
    }

    public void ReplaceFirstPokemon(Pokemon pokemon)
    {
        if (_team.Length == 0)
        {
            return;
        }

        _team[0] = pokemon;
        ActiveIndex = 0;
        EnsureActivePokemonIsValid();
    }

    public void SetPokemonAt(int index, Pokemon pokemon)
    {
        if (index < 0 || index >= _team.Length)
        {
            return;
        }

        _team[index] = pokemon;
        if (ActiveIndex < 0 || ActiveIndex == index)
        {
            ActiveIndex = index;
        }

        EnsureActivePokemonIsValid();
    }

    public void LoadTeam(Pokemon[] source, int activeIndex)
    {
        for (int i = 0; i < _team.Length; i++)
        {
            _team[i] = (source != null && i < source.Length) ? source[i] : null;
        }

        ActiveIndex = activeIndex;
        EnsureActivePokemonIsValid();
    }

    public bool IsTeamFull()
    {
        return FirstEmptySlot() < 0;
    }

    public bool HasHealthyPokemon()
    {
        for (int i = 0; i < _team.Length; i++)
        {
            Pokemon candidate = _team[i];
            if (candidate != null && candidate.Hp > 0)
            {
                return true;
            }
        }

        return false;
    }

    public Pokemon SelectAvailablePokemon(bool includeCurrentSlot)
    {
        if (_team.Length == 0)
        {
            ActiveIndex = -1;
            return null;
        }

        int currentIndex = ActiveIndex;
        if (currentIndex < 0 || currentIndex >= _team.Length)
        {
            currentIndex = 0;
        }

        int searchStart = includeCurrentSlot ? currentIndex : ((currentIndex + 1) % _team.Length);

        for (int offset = 0; offset < _team.Length; offset++)
        {
            int idx = (searchStart + offset) % _team.Length;
            Pokemon candidate = _team[idx];
            if (candidate != null && candidate.Hp > 0)
            {
                ActiveIndex = idx;
                return candidate;
            }
        }

        ActiveIndex = -1;
        return null;
    }

    public void SetActiveIndex(int index)
    {
        ActiveIndex = index;
        EnsureActivePokemonIsValid();
    }

    private void EnsureActivePokemonIsValid()
    {
        if (ActiveIndex >= 0 && ActiveIndex < _team.Length)
        {
            Pokemon candidate = _team[ActiveIndex];
            if (candidate != null && candidate.Hp > 0)
            {
                return;
            }
        }

        for (int i = 0; i < _team.Length; i++)
        {
            Pokemon candidate = _team[i];
            if (candidate != null && candidate.Hp > 0)
            {
                ActiveIndex = i;
                return;
            }
        }

        ActiveIndex = -1;
    }
}
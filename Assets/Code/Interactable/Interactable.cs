using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Interactable Tile", menuName = "TilesSet/Interactable Tile", order = 1)]
public class Interactable : ScriptableObject
{
    [Header("Interaction Settings")]
    public string interactionText = "Press E to interact";
    public bool canInteractMultipleTimes = true;
    public float interactionCooldown = 0.5f;

    [Header("Effects")]
    public bool destroyOnInteract = false;
    public GameObject spawnOnInteract;
    public AudioClip interactionSound;

    [Header("Rewards")]
    public int experienceReward = 0;
    public GameObject[] itemsToSpawn;

    [TextArea(3, 5)]
    public string dialogueText = "";

    // Virtual method that can be overridden in derived classes
    public virtual void OnInteract(Vector3Int tilePosition, GameObject player)
    {
        Debug.Log($"Interacting with {name} at position {tilePosition}");

        // Default interaction behavior
        if (!string.IsNullOrEmpty(dialogueText))
        {
            // Show dialogue (you'll need to implement your dialogue system)
            ShowDialogue(dialogueText);
        }

        if (experienceReward > 0)
        {
            // Give experience (implement your experience system)
            GiveExperience(player, experienceReward);
        }

        if (itemsToSpawn.Length > 0)
        {
            SpawnItems(tilePosition, itemsToSpawn);
        }

        if (spawnOnInteract != null)
        {
            Instantiate(spawnOnInteract, tilePosition, Quaternion.identity);
        }

        PlayInteractionSound();
    }

    protected virtual void ShowDialogue(string text)
    {
        // Implement your dialogue system here
        Debug.Log($"Dialogue: {text}");
    }

    protected virtual void GiveExperience(GameObject player, int amount)
    {
        // Implement your experience system here
        Debug.Log($"Player gained {amount} experience!");
    }

    protected virtual void SpawnItems(Vector3Int position, GameObject[] items)
    {
        foreach (var item in items)
        {
            if (item != null)
            {
                Vector3 spawnPos = new Vector3(position.x + Random.Range(-0.5f, 0.5f),
                                             position.y + Random.Range(-0.5f, 0.5f),
                                             position.z);
                Instantiate(item, spawnPos, Quaternion.identity);
            }
        }
    }

    protected virtual void PlayInteractionSound()
    {
        if (interactionSound != null)
        {
            // You'll need an AudioSource component somewhere
            AudioSource.PlayClipAtPoint(interactionSound, Camera.main.transform.position);
        }
    }
}

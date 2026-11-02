import { useEffect, useState } from "react";

type Address = {
  id: number;
  label: string;
  street: string;
  city: string;
  isDefault: boolean;
};

export default function AddressList() {
  const [addresses, setAddresses] = useState<Address[]>([]);
  const [message, setMessage] = useState("");
  const [error, setError] = useState("");

  // fetch only rejects on network failures, so an error status has to be checked by hand.
  async function loadAddresses() {
    const res = await fetch("/api/addresses");
    if (!res.ok) throw new Error(`GET /api/addresses failed with ${res.status}`);
    setAddresses(await res.json());
  }

  useEffect(() => {
    loadAddresses().catch(() => setError("Could not load your addresses"));
  }, []);

  async function makeDefault(id: number) {
    setMessage("");
    setError("");
    try {
      const res = await fetch(`/api/addresses/${id}/default`, { method: "PUT" });
      if (!res.ok) throw new Error(`PUT /api/addresses/${id}/default failed with ${res.status}`);
      // Reload from the API so the list shows what was saved: one default, listed first.
      await loadAddresses();
      setMessage("Default address updated");
    } catch {
      setError("Could not update the default address");
    }
  }

  return (
    <main>
      <h1>My addresses</h1>
      <p>Signed in as customer 1</p>
      {message && <p role="status">{message}</p>}
      {error && <p role="alert">{error}</p>}
      <table>
        <thead>
          <tr>
            <th>Label</th>
            <th>Street</th>
            <th>City</th>
            <th>Default</th>
            <th></th>
          </tr>
        </thead>
        <tbody>
          {addresses.map((a) => (
            <tr key={a.id}>
              <td>{a.label}</td>
              <td>{a.street}</td>
              <td>{a.city}</td>
              <td>{a.isDefault ? "Default" : ""}</td>
              <td>{!a.isDefault && <button onClick={() => makeDefault(a.id)}>Make default</button>}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </main>
  );
}
